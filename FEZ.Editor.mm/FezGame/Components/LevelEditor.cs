using FezGame.Mod;
using FezEngine.Mod;
using System;
using System.Collections.Generic;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using FezGame.Editor;
using FezGame.Mod.Gui;
using System.Drawing.Imaging;
using Microsoft.Xna.Framework.Input;
using FezEngine.Effects;
using FezEngine.Structure.Geometry;
#if FNA
using Microsoft.Xna.Framework.Input;
using SDL2;
#endif

namespace FezGame.Components {
    public partial class LevelEditor : DrawableGameComponent, ILevelEditor, IGuiHandler {

        [ServiceDependency]
        public IMouseStateManager MouseState { get; set; }
        [ServiceDependency]
        public ISoundManager SoundManager { get; set; }
        [ServiceDependency]
        public IInputManager InputManager { get; set; }
        [ServiceDependency]
        public IKeyboardStateManager KeyboardState { get; set; }
        [ServiceDependency]
        public IGameService GameService { get; set; }
        [ServiceDependency]
        public IGameStateManager GameState { get; set; }
        [ServiceDependency]
        public IGameCameraManager CameraManager { get; set; }
        [ServiceDependency]
        public IFontManager FontManager { get; set; }
        [ServiceDependency]
        public IPlayerManager PlayerManager { get; set; }
        [ServiceDependency]
        public IGameLevelManager LevelManager { get; set; }
        [ServiceDependency]
        public ILevelMaterializer LevelMaterializer { get; set; }
        [ServiceDependency]
        public IDotManager DotManager { get; set; }
        [ServiceDependency]
        public ISpeechBubbleManager SpeechBubble { get; set; }
        [ServiceDependency]
        public IContentManagerProvider CMProvider { get; set; }
        [ServiceDependency]
        public ITargetRenderingManager TRM { get; set; }

        public static LevelEditor Instance;
        
        #if !FNA
        public static readonly float WheelTurnsFactor = 1f;
        #else
        public static readonly float WheelTurnsFactor = 1f / 64f;
        #endif

        public SpriteBatch SpriteBatch { get; set; }
        public GlyphTextRenderer GTR { get; set; }

        protected float SinceMouseMoved = 3f;
        protected bool CursorHovering = false;
        protected Texture2D CursorDefault;
        protected Texture2D CursorClick;
        protected Texture2D CursorHover;
        protected Texture2D CursorAction;
        protected Vector2 CursorOffset = new Vector2(16f, 16f);
        protected Color CursorColor = Color.White;
        
        public Point DragOrigin;
        
        public Color SelectColor = new Color(0.3f, 0.7f, 1f);
        public bool Selecting = false;
        public int SelectingMin = 12;
        public Color UnselectColor = new Color(1f, 0.3f, 0.3f);
        public float SelectRayDistance = 8f;
        
        public Color DraggingColor = new Color(0.3f, 1f, 0.3f);
        protected Vector3 DraggingOrigin;
        protected float DraggingAOGrid = 0.0625f;
        protected ArtObjectInstance DraggingAO;
        
        public Vector3 SelectDiffuse = new Vector3(0.3f, 0.7f, 1f);
        public float SelectAlpha = 1f;
        
        public List<TrileInstance> SelectedTriles = new List<TrileInstance>();
        public Mesh[] SelectedMeshes;
        public static BaseEffect SelectedEffect;
        
        public Vector3 PlacingDiffuse = new Vector3(0.5f, 0.5f, 0.5f);
        public float PlacingAlpha = 0.625f;
        
        public Mesh PlacingMesh;
        public Mesh[] PlacingMeshes;
        public static BaseEffect PlacingEffect;
        
        
        public Vector2 FakeFreeLook;
        

        protected int SkipLoading = 0;

        protected KeyValuePair<TrileEmplacement, TrileInstance>[] tmpTriles = new KeyValuePair<TrileEmplacement, TrileInstance>[8192];
        protected int trilesCount = 0;
        protected KeyValuePair<int, ArtObjectInstance>[] tmpAOs = new KeyValuePair<int, ArtObjectInstance>[128];

        public TrileInstance HoveredTrile { get; set; }
        public ArtObjectInstance HoveredAO { get; set; }
        public BoundingBox HoveredBox { get; set; }
        public FaceOrientation HoveredFace { get; set; }
        protected object placing;
        public object Placing {
            get {
                return placing;
            }
            set {
                if (placing != value) {
                    if (PlacingMeshes != null) {
                        for (int i = 0; i < PlacingMeshes.Length; i++) {
                            PlacingMeshes[i].Dispose(false);
                        }
                    }
                    PlacingMeshes = null;
                    if (PlacingMesh != null) {
                        PlacingMesh.Dispose(false);
                    }
                    PlacingMesh = null;
                    
                    if (value is Trile) {
                        PlacingMesh = GenMesh((Trile) value, PlacingPhi, PlacingEffect);
                    } else if (value is ArtObject) {
                        PlacingMesh = GenMesh((ArtObject) value, PlacingPhi, PlacingEffect);
                    }
                }
                
                placing = value;
            }
        }
        public float PlacingPhiOffset = 0f;
        public float PlacingPhi {
            get {
                float offs = MathHelper.Pi * PlacingPhiOffset / 2f;
                if (CameraManager.Viewpoint.IsOrthographic()) {
                    return FezMath.SnapPhi(CameraManager.Viewpoint.ToPhi() + offs);
                } else {
                    //TODO get closest viewpoint... somehow.
                }
                return FezMath.SnapPhi(offs);
            }
            set {
                float cam = 0f;
                if (CameraManager.Viewpoint.IsOrthographic()) {
                    cam = FezMath.SnapPhi(CameraManager.Viewpoint.ToPhi());
                } else {
                    //TODO get closest viewpoint... somehow.
                }
                PlacingPhiOffset = (value - cam) * 2f / MathHelper.Pi;
            }
        }
        
        public Vector3 PlacingOffset {
            get {
                if (Placing is ArtObject) {
                    return -HoveredFace.AsVector() * ((ArtObject) Placing).Size / 2f;
                }
                return -HoveredFace.AsVector();
            }
        }

        public bool ThumbnailScheduled { get; set; }
        public int ThumbnailX { get; set; }
        public int ThumbnailY { get; set; }
        public int ThumbnailSize { get; set; }
        protected RenderTargetHandle ThumbnailRT;
        
        protected static Texture2D pixelTexture_;
        protected Texture2D pixelTexture {
            get {
                if (pixelTexture_ == null) {
                    pixelTexture_ = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                    pixelTexture_.SetData<Color>(new Color[] { Color.White });
                }
                return pixelTexture_;
            }
        }
        
        public LevelEditor(Game game)
            : base(game) {
            UpdateOrder = -10;
            DrawOrder = 4000;
            Placing = null;
            Instance = this;
        }

        #if FNA
        public void OnTextInput(char c) {
        #else
        public void OnTextInput(Object sender, TextInputEventArgs e) {
        char c = e.Character;
        #endif
            if (FocusedWidget != null) {
                FocusedWidget.TextInput(c);
            }
        }

        public override void Initialize() {
            base.Initialize();

            Scheduled = new List<Action>();

            #if FNA
            TextInputEXT.TextInput += OnTextInput;
            //SDL.SDL_StartTextInput(); //FIXME: Only enable text input when a text field is selected!
            #else
            Game.Window.TextInput += OnTextInput;
            #endif
            
            KeyboardState.RegisterKey(Keys.LeftControl);
            KeyboardState.RegisterKey(Keys.S);
            KeyboardState.RegisterKey(Keys.N);
            KeyboardState.RegisterKey(Keys.O);
            
            Vector2 tmpFreeLook = new Vector2(0f, 0f);
            Func<Vector2> prev_get_FreeLook = FakeInputHelper.get_FreeLook;
            Action<Vector2> prev_set_FreeLook = FakeInputHelper.set_FreeLook;
            FakeInputHelper.get_FreeLook = delegate() {
                return FakeInputHelper.Updating ? (prev_get_FreeLook != null ? prev_get_FreeLook() : tmpFreeLook) : FakeFreeLook;
            };
            FakeInputHelper.set_FreeLook = delegate(Vector2 value) {
                tmpFreeLook = value;
                if (prev_set_FreeLook != null) {
                    prev_set_FreeLook(value);
                }
            };
            
            FEZMod.FEZometric = true;
            FEZMod.DisableInventory = true;
            
            SelectedEffect = PlacingEffect = new CubemappedEffect();

            SetupGui();

        }

        public void Preload() {
            GTR = new GlyphTextRenderer(Game);

            CursorDefault = CMProvider.Global.Load<Texture2D>("editor/cursor/DEFAULT");
            CursorClick = CMProvider.Global.Load<Texture2D>("editor/cursor/CLICK");
            CursorHover = CMProvider.Global.Load<Texture2D>("editor/cursor/HOVER");
            CursorAction = CMProvider.Global.Load<Texture2D>("editor/cursor/ACTION");
            
            AllButtonWidget.TexAll = CMProvider.Global.Load<Texture2D>("editor/ALL");
            AllButtonWidget.TexHideAll = CMProvider.Global.Load<Texture2D>("editor/HIDEALL");

            SpriteBatch = new SpriteBatch(GraphicsDevice);
        }

        public override void Update(GameTime gameTime) {
            while (Scheduled.Count > 0) {
                Scheduled[0]();
                Scheduled.RemoveAt(0);
            }

            if ((--SkipLoading) == 0) {
                GameState.Loading = false;
                return;
            }

            if (GameState.Loading || GameState.InMap || GameState.InMenuCube || GameState.Paused || string.IsNullOrEmpty(LevelManager.Name)) {
                return;
            }

            if (ThumbnailScheduled) {
                if (ThumbnailRT == null) {
                    ThumbnailRT = TRM.TakeTarget();
                    TRM.ScheduleHook(DrawOrder, ThumbnailRT.Target);
                }
                return;
            }

            SinceMouseMoved += (float) gameTime.ElapsedGameTime.TotalSeconds;
            if (MouseState.Movement.X != 0 || MouseState.Movement.Y != 0) {
                SinceMouseMoved = 0f;
            }
            
            //Ugly thread-safety workaround
            trilesCount = LevelManager.Triles.Count;
            LevelManager.Triles.CopyTo(tmpTriles.Length < trilesCount ? (tmpTriles = new KeyValuePair<TrileEmplacement, TrileInstance>[trilesCount]) : tmpTriles, 0);
            
            Ray ray = new Ray(
                GraphicsDevice.Viewport.Unproject(
                    new Vector3(MouseState.Position.X, MouseState.Position.Y, 0.0f),
                    CameraManager.Projection, CameraManager.View, Matrix.Identity
                ),
                CameraManager.InverseView.Forward
            );
            
            float intersectionMin = float.MaxValue;
            HoveredTrile = GetTrile(ray, ref intersectionMin);
            if (HoveredTrile != null) {
                HoveredBox = new BoundingBox(HoveredTrile.Position, HoveredTrile.Position + Vector3.One);
            }
            
            HoveredAO = null;
            int aosCount = LevelManager.ArtObjects.Count;
            LevelManager.ArtObjects.CopyTo(tmpAOs.Length < aosCount ? (tmpAOs = new KeyValuePair<int, ArtObjectInstance>[aosCount]) : tmpAOs, 0);
            for (int i = 0; i < aosCount; i++) {
                ArtObjectInstance ao = tmpAOs[i].Value;
                float? intersection = ray.Intersects(ao.Bounds);
                if (intersection.HasValue && intersection < intersectionMin) {
                    HoveredTrile = null;
                    HoveredAO = ao;
                    HoveredBox = ao.Bounds;
                    intersectionMin = intersection.Value;
                }
            }
            
            Selecting = false;
            CursorColor = Color.White;
            if (MouseState.LeftButton.State == MouseButtonStates.DragStarted ||
                MouseState.MiddleButton.State == MouseButtonStates.DragStarted ||
                MouseState.RightButton.State == MouseButtonStates.DragStarted) {
                DragOrigin = MouseState.Position;
            }
            
            if ((MouseState.LeftButton.State != MouseButtonStates.DragStarted && 
                MouseState.LeftButton.State != MouseButtonStates.Dragging &&
                MouseState.LeftButton.State != MouseButtonStates.DragEnded)
                || DraggingWidget != null) {
                DraggingAO = null;
            }
            
            if ((MouseState.MiddleButton.State == MouseButtonStates.DragStarted || 
                MouseState.MiddleButton.State == MouseButtonStates.Dragging ||
                MouseState.MiddleButton.State == MouseButtonStates.DragEnded)
                && DraggingWidget == null) {
                HoveredTrile = null;
                HoveredAO = null;
                DraggingAO = null;
                
                FakeFreeLook.X = -(MouseState.Movement.X / 3.25f); //Should the X rotation be inversed here or in the cam?
                FakeFreeLook.Y = MouseState.Movement.Y / 3.25f;
                
                if (MouseState.MiddleButton.State == MouseButtonStates.DragEnded) {
                    FakeFreeLook = Vector2.Zero;
                }
                
                
            } else if ((MouseState.LeftButton.State == MouseButtonStates.DragStarted || 
                MouseState.LeftButton.State == MouseButtonStates.Dragging ||
                MouseState.LeftButton.State == MouseButtonStates.DragEnded)
                && DraggingWidget == null) {
                
                if (MouseState.LeftButton.State == MouseButtonStates.DragStarted && HoveredAO != null) {
                    CursorColor = DraggingColor;
                    DraggingAO = HoveredAO;
                    DraggingOrigin = DraggingAO.Position;
                    PlacingPhi = DraggingAO.Rotation.ToPhi();
                }
                
                HoveredTrile = null;
                HoveredAO = null;
                
                
                if (DraggingAO == null &&
                    (SelectingMin < Math.Abs(MouseState.Position.X - DragOrigin.X) ||
                    SelectingMin < Math.Abs(MouseState.Position.Y - DragOrigin.Y))) {
                    Selecting = true;
                    CursorColor = SelectColor;
                    
                    if (MouseState.LeftButton.State == MouseButtonStates.DragEnded) {
                        List<TrileInstance> unselected = null;
                        if (KeyboardState.GetKeyState(Keys.LeftControl) == FezButtonState.Down) {
                            SelectedTriles = GetTrilesSelected(SelectedTriles);
                        } else {
                            unselected = SelectedTriles;
                            SelectedTriles = GetTrilesSelected();
                        }
                        
                        UpdateSelection(unselected);
                    }
                    
                    
                } else if (DraggingAO != null) {
                    CursorColor = DraggingColor;
                    Vector3 dragOriginWorld = GraphicsDevice.Viewport.Unproject(
                        new Vector3(DragOrigin.X, DragOrigin.Y, 0.0f),
                        CameraManager.Projection, CameraManager.View, Matrix.Identity
                    );
                    DraggingAO.Position = DraggingOrigin + ray.Position - dragOriginWorld;
                    DraggingAO.Position = new Vector3(
                        (float) Math.Floor(DraggingAO.Position.X / DraggingAOGrid) * DraggingAOGrid,
                        (float) Math.Floor(DraggingAO.Position.Y / DraggingAOGrid) * DraggingAOGrid,
                        (float) Math.Floor(DraggingAO.Position.Z / DraggingAOGrid) * DraggingAOGrid
                    );
                    DraggingAO.Rotation = FezMath.QuaternionFromPhi(PlacingPhi);
                }
                
                
            } else if ((MouseState.RightButton.State == MouseButtonStates.DragStarted || 
                MouseState.RightButton.State == MouseButtonStates.Dragging ||
                MouseState.RightButton.State == MouseButtonStates.DragEnded)
                && DraggingWidget == null &&
                (SelectingMin < Math.Abs(MouseState.Position.X - DragOrigin.X) ||
                SelectingMin < Math.Abs(MouseState.Position.Y - DragOrigin.Y))) {
                HoveredTrile = null;
                HoveredAO = null;
                DraggingAO = null;
                Selecting = true;
                CursorColor = UnselectColor;
                
                if (MouseState.RightButton.State == MouseButtonStates.DragEnded && SelectedTriles != null) {
                    List<TrileInstance> unselected = GetTrilesSelected();
                    SelectedTriles.RemoveAll((trile) => unselected.Contains(trile));
                    
                    UpdateSelection(unselected);
                }
                
                
            } else if (HoveredTrile != null && MouseState.LeftButton.State == MouseButtonStates.Clicked &&
                KeyboardState.GetKeyState(Keys.LeftControl) == FezButtonState.Down) {
                
                if (SelectedTriles != null && !SelectedTriles.Contains(HoveredTrile)) {
                    SelectedTriles.Add(HoveredTrile);
                    UpdateSelection();
                }
                
                HoveredTrile = null;
            } else if (HoveredTrile != null && MouseState.RightButton.State == MouseButtonStates.Clicked &&
                KeyboardState.GetKeyState(Keys.LeftControl) == FezButtonState.Down) {
                
                if (SelectedTriles != null) {
                    SelectedTriles.Remove(HoveredTrile);
                    List<TrileInstance> unselected = EditorUtils.l_TrileInstance.GetNext();
                    unselected.Clear();
                    unselected.Add(HoveredTrile);
                    UpdateSelection(unselected);
                }
                
                HoveredTrile = null;
            }

            AssetPickerWidget.Position.Y = GraphicsDevice.Viewport.Height - AssetPickerWidget.Size.Y;
            InfoWidget.Position.Y = AssetPickerWidget.Position.Y - InfoWidget.Size.Y;
            AssetPickerPickerWidget.Visible = AssetPickerWidget.Large;
            AssetPickerPickerWidget.Background.A = 0;
            AssetPickerPickerWidget.Position.X = InfoWidget.Position.X + InfoWidget.Size.X;
            AssetPickerPickerWidget.Position.Y = AssetPickerWidget.Position.Y - AssetPickerPickerWidget.Size.Y;
            AssetPickerPickerWidget.Size.X = GraphicsDevice.Viewport.Width - InfoWidget.Size.X;
            float pickerLabelXOffs = 0f;
            for (int i = 0; i < AssetPickerWidgets.Count; i++) {
                AssetPickerWidget picker = AssetPickerWidgets[i];
                picker.Visible = picker == AssetPickerWidget;
                picker.Large = AssetPickerWidget.Large;
                
                ButtonWidget pickerLabel = AssetPickerLabels[i];
                pickerLabel.Position.X = pickerLabelXOffs;
                pickerLabelXOffs += pickerLabel.Size.X + 4f;
                
                if (pickerLabel.Action == null) {
                    pickerLabel.Action = () => AssetPickerWidget = picker;
                }
            }

            bool cursorInMenu = UpdateWidgets(gameTime, Widgets, true);

            if (DraggingWidget != null && (MouseState.LeftButton.State == MouseButtonStates.Dragging || MouseState.LeftButton.State == MouseButtonStates.DragEnded)) {
                DraggingWidget.Dragging(gameTime, MouseState.LeftButton.State);
                cursorInMenu = true;

                if (MouseState.LeftButton.State == MouseButtonStates.DragEnded) {
                    DraggingWidget = null;
                }
            }

            CursorHovering = false;
            if (cursorInMenu) {
                HoveredTrile = null;
                HoveredAO = null;
                return;
            }

            if (HoveredTrile != null) {
                HoveredFace = GetHoveredFace(HoveredBox, ray);
                CursorHovering = true;
            }

            if (MouseState.LeftButton.State == MouseButtonStates.Clicked) {
                bool unfocusWidget = true;

                if (HoveredTrile != null) {
                    TrileEmplacement emplacement = new TrileEmplacement(HoveredTrile.Position + PlacingOffset);
                    if (FocusedWidget is TextFieldWidget) {
                        ((TextFieldWidget) FocusedWidget).Text = emplacement.X + "; " + emplacement.Y + "; " + emplacement.Z;
                        unfocusWidget = false;
                    } else if (LevelManager.TrileSet != null && Placing is Trile) {
                        AddTrile(CreateNewTrile(((Trile) Placing).Id, emplacement));
                    } else if (Placing is ArtObject) {
                        ArtObject ao_ = (ArtObject) Placing;
                        
                        int maxID = 0;
                        foreach (int id in LevelManager.ArtObjects.Keys) {
                            if (id >= maxID) {
                                maxID = id + 1;
                            }
                        }
                        
                        ArtObjectInstance ao = new ArtObjectInstance(ao_.Name) {
                            Id = maxID,
                            Position = HoveredTrile.Position + PlacingOffset,
                            Rotation = FezMath.QuaternionFromPhi(PlacingPhi),
                            Scale = Vector3.One
                        };
                        ao.ActorSettings = new ArtObjectActorSettings() {
                            RotationCenter = ao_.Size / 2
                        };
                        ao.ArtObject = ao_;
                        ao.Initialize();
                        LevelManager.ArtObjects[ao.Id] = ao;
                        LevelMaterializer.RegisterSatellites();
                    }
                }

                if (unfocusWidget) {
                    if (FocusedWidget != null) {
                        FocusedWidget.Unfocus(gameTime);
                    }
                    FocusedWidget = null;
                }
            }

            if (MouseState.RightButton.State == MouseButtonStates.Clicked && HoveredTrile != null) {
                LevelManager.ClearTrile(HoveredTrile);
                LevelMaterializer.CullInstances();
                HoveredTrile = null;
            }

            if (MouseState.MiddleButton.State == MouseButtonStates.Clicked && HoveredTrile != null) {
                Placing = HoveredTrile.Trile;
            }

            if (HoveredTrile == null && DraggingAO == null) {
                CameraManager.PixelsPerTrixel = Math.Max(0.25f, CameraManager.PixelsPerTrixel + 0.25f * MouseState.WheelTurns * WheelTurnsFactor);
            } else {
                PlacingPhiOffset = (PlacingPhiOffset + MouseState.WheelTurns / 120f) % 4;
            }

            if (FocusedWidget == null && KeyboardState.GetKeyState(Keys.LeftControl) == FezButtonState.Down) {
                if (KeyboardState.GetKeyState(Keys.S) == FezButtonState.Pressed) {
                    Save(true, true);
                } else if (KeyboardState.GetKeyState(Keys.N) == FezButtonState.Pressed) {
                    TopBarWidget.Widgets[0 /*File*/].Widgets[0 /*New*/].Click(gameTime, 1);
                } else if (KeyboardState.GetKeyState(Keys.O) == FezButtonState.Pressed) {
                    TopBarWidget.Widgets[0 /*File*/].Hover(gameTime);
                    TopBarWidget.Widgets[0 /*File*/].Widgets[1 /*Open*/].Hover(gameTime);
                    FocusedWidget = TopBarWidget.Widgets[0 /*File*/].Widgets[1 /*Open*/].Widgets[0 /*Field*/];
                    FocusedWidget.Click(gameTime, 1);
                }
            }
        }
        
        public override void Draw(GameTime gameTime) {
            if (GameState.Loading || GameState.InMap || GameState.InMenuCube || GameState.Paused || !FEZMod.Preloaded || string.IsNullOrEmpty(LevelManager.Name)) {
                return;
            }

            if (ThumbnailScheduled) {
                if (ThumbnailRT == null) {
                    return;
                }

                string filePath = ("other textures/map_screens/" + LevelManager.Name).Externalize() + ".png";
                Directory.GetParent(filePath).Create();
                TRM.Resolve(ThumbnailRT.Target, false);
                using (System.Drawing.Bitmap bitmap = ThumbnailRT.Target.ToBitmap()) {
                    //float x = ThumbnailRT.Target.Width / 2 - ThumbnailSize / 2f;
                    //float y = ThumbnailRT.Target.Height / 2 - ThumbnailSize / 2f;
                    using (System.Drawing.Bitmap thumbnail = bitmap.Clone(new System.Drawing.Rectangle(ThumbnailX, ThumbnailY, ThumbnailSize, ThumbnailSize), bitmap.PixelFormat)) {
                        using (FileStream fs = new FileStream(filePath, FileMode.Create)) {
                            thumbnail.Save(fs, ImageFormat.Png);
                        }
                    }
                }
                TRM.ReturnTarget(ThumbnailRT);
                ThumbnailRT = null;
                ThumbnailScheduled = false;
                FEZMod.CreatingThumbnail = false;
                //maybe show a flash or something; in the meantime simply don't render the editor
                return;
            }
            
            if (SelectedMeshes != null) {
                for (int i = 0; i < SelectedMeshes.Length; i++) {
                    Mesh mesh = SelectedMeshes[i];
                    mesh.Blending = BlendingMode.Alphablending;
                    mesh.Material.Opacity = SelectAlpha;
                    mesh.Material.Diffuse = SelectDiffuse;
                    mesh.Draw();
                }
            }
            
            if (HoveredTrile != null && PlacingMesh != null) {
                PlacingMesh.Blending = BlendingMode.Alphablending;
                PlacingMesh.Material.Opacity = PlacingAlpha;
                PlacingMesh.Material.Diffuse = PlacingDiffuse;
                if (Placing is Trile) {
                    PlacingMesh.Position = HoveredTrile.Center + PlacingOffset;
                    PlacingMesh.SetRotation((Trile) Placing, PlacingPhi);
                    PlacingMesh.Draw();
                } else if (Placing is ArtObject) {
                    PlacingMesh.Position = HoveredTrile.Position + PlacingOffset;
                    PlacingMesh.SetRotation(PlacingPhi);
                    PlacingMesh.Draw();
                }
            }

            Viewport viewport = GraphicsDevice.Viewport;
            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);

            float cursorScale = viewScale * 2f;
            Point cursorPosition = SettingsManager.PositionInViewport(MouseState);
            Texture2D cursor;
            if (MouseState.LeftButton.State == MouseButtonStates.Dragging || MouseState.RightButton.State == MouseButtonStates.Dragging ||
                MouseState.LeftButton.State == MouseButtonStates.Clicked || MouseState.RightButton.State == MouseButtonStates.Clicked ||
                MouseState.LeftButton.State == MouseButtonStates.Down || MouseState.RightButton.State == MouseButtonStates.Down) {
                if (CursorHovering) {
                    cursor = CursorAction;
                } else {
                    cursor = CursorClick;
                }
            } else if (CursorHovering) {
                cursor = CursorHover;
            } else {
                cursor = CursorDefault;
            }

            GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
            SpriteBatch.BeginPoint();

            if (Selecting) {
                float x = DragOrigin.X;
                float y = DragOrigin.Y;
                float w = MouseState.Position.X - DragOrigin.X;
                float h = MouseState.Position.Y - DragOrigin.Y;
                if (w < 0) {
                    x += w;
                    w = -w;
                }
                if (h < 0) {
                    y += h;
                    h = -h;
                }
                SpriteBatch.Draw(pixelTexture, 
                    new Vector2(
                        x,
                        y
                    ), null,
                    CursorColor * 0.7f,
                    0.0f,
                    Vector2.Zero,
                    new Vector2(
                        w,
                        h
                    ), SpriteEffects.None,
                    0.0f);
            }

            foreach (GuiWidget widget in Widgets) {
                widget.GuiHandler = this;
                widget.Draw(gameTime);
            }

            SpriteBatch.Draw(cursor, 
                new Vector2(
                    (float) cursorPosition.X - cursorScale * CursorOffset.X,
                    (float) cursorPosition.Y - cursorScale * CursorOffset.Y
                ), null,
                CursorColor * FezMath.Saturate((float) (1.0 - ((double) SinceMouseMoved - 2.0))),
                0.0f,
                Vector2.Zero,
                cursorScale,
                SpriteEffects.None,
                0.0f);

            SpriteBatch.End();
        }

        protected bool UpdateWidgets(GameTime gameTime, List<GuiWidget> widgets, Boolean update) {
            bool cursorOnWidget = false;
            for (int i = widgets.Count - 1; i >= 0; i--) {
                GuiWidget widget = widgets[i];
                widget.GuiHandler = this;
                if (update && widget.Visible) {
                    widget.PreUpdate();
                    widget.Update(gameTime);
                }
                if (!widget.Visible) {
                    continue;
                }
                bool cursorOnChild = cursorOnWidget;
                if (widget.ShowChildren) {
                    cursorOnChild = cursorOnWidget || UpdateWidgets(gameTime, widget.Widgets, false);
                }
                if (widget.InView && (widget.Position.X + widget.Offset.X <= MouseState.Position.X && MouseState.Position.X <= widget.Position.X + widget.Offset.X + widget.Size.X &&
                    widget.Position.Y + widget.Offset.Y <= MouseState.Position.Y && MouseState.Position.Y <= widget.Position.Y + widget.Offset.Y + widget.Size.Y)) {
                    cursorOnWidget = true;
                    if (!cursorOnChild) {
                        widget.Hover(gameTime);
                    }
                    if (!cursorOnChild && MouseState.LeftButton.State == MouseButtonStates.Clicked) {
                        widget.Click(gameTime, 1);
                        if (FocusedWidget != null) {
                            FocusedWidget.Unfocus(gameTime);
                        }
                        FocusedWidget = widget;
                    }
                    if (!cursorOnChild && MouseState.RightButton.State == MouseButtonStates.Clicked) {
                        widget.Click(gameTime, 3);
                    }
                    if (!cursorOnChild && MouseState.LeftButton.State == MouseButtonStates.DragStarted) {
                        if (DraggingWidget != null) {
                            DraggingWidget.Dragging(gameTime, MouseButtonStates.DragEnded);
                        }
                        DraggingWidget = widget;
                        DraggingWidget.Dragging(gameTime, MouseButtonStates.DragStarted);
                    }
                    widget.Scroll(gameTime, (int) (MouseState.WheelTurns * WheelTurnsFactor));
                }
                cursorOnWidget = cursorOnWidget || cursorOnChild;
            }
            return cursorOnWidget;
        }
        
        public void UpdateSelection(List<TrileInstance> unselected = null) {
            if (unselected != null) {
                for (int i = 0; i < unselected.Count; i++) {
                    TrileInstance trile = unselected[i];
                    trile.Foreign = false;
                    trile.Hidden = false;
                    LevelManager.RecullAt(trile);
                }
            }
            
            if (SelectedTriles.Count == 0) {
                //TODO remove selection widget
                
                SelectedMeshes = null;
                LevelMaterializer.RebuildInstances();
                CameraManager.RebuildView();
                if (CameraManager is DefaultCameraManager) {
                    ((DefaultCameraManager) CameraManager).RebuildProjection();
                }
                return;
            }
            
            //TODO add / update selection widget
            
            //Simply rebuild the mesh list
            if (SelectedMeshes != null) {
                for (int i = 0; i < SelectedMeshes.Length; i++) {
                    SelectedMeshes[i].Dispose(false);
                }
            }
            SelectedMeshes = new Mesh[SelectedTriles.Count];
            
            for (int i = 0; i < SelectedTriles.Count; i++) {
                TrileInstance trile = SelectedTriles[i];
                trile.Foreign = true;
                trile.Hidden = true;
                
                Mesh mesh = SelectedMeshes[i] = GenMesh(trile, SelectedEffect);
                mesh.Position = trile.Center;
                
                LevelManager.RecullAt(trile);
            }
            
            CameraManager.RebuildView();
            if (CameraManager is DefaultCameraManager) {
                ((DefaultCameraManager) CameraManager).RebuildProjection();
            }
        }

        public Level CreateNewLevel(string name, int width, int height, int depth, string trileset) {
            Level level = new Level();
            level.Name = name;
            level.Size = new Vector3(width, height, depth);
            level.TrileSetName = trileset;
            level.StartingPosition = new TrileFace();
            level.StartingPosition.Face = FaceOrientation.Front;
            level.StartingPosition.Id = new TrileEmplacement(width / 2, height / 2, depth / 2);
            PlayerManager.Position = level.StartingPosition.Id.AsVector;
            return level;
        }

        public TrileInstance CreateNewTrile(int trileId, TrileEmplacement emplacement) {
            TrileInstance trile = new TrileInstance(emplacement, trileId);
            LevelManager.Triles[emplacement] = trile;

            trile.SetPhiLight(PlacingPhi);

            trile.PhysicsState = new InstancePhysicsState(trile) {
                Respawned = true,
                Vanished = false
            };
            trile.Enabled = true;

            return trile;
        }
        
        public TrileInstance GetTrile(Ray ray, ref float intersectionMin) {
            TrileInstance trileBest = null;
            for (int i = 0; i < trilesCount; i++) {
                TrileInstance trile = tmpTriles[i].Value;
                BoundingBox box = new BoundingBox(trile.Position, trile.Position + Vector3.One);
                float? intersection = ray.Intersects(box);
                if (intersection.HasValue && intersection < intersectionMin) {
                    trileBest = trile;
                    intersectionMin = intersection.Value;
                }
            }
            return trileBest;
        }
        
        public TrileInstance GetTrile(Ray ray) {
            float intersectionMin = float.MaxValue;
            return GetTrile(ray, ref intersectionMin);
        }
        
        public List<TrileInstance> GetTriles(Ray ray, List<TrileInstance> trilesGot = null) {
            if (trilesGot == null) {
                trilesGot = new List<TrileInstance>();
            }
            for (int i = 0; i < trilesCount; i++) {
                TrileInstance trile = tmpTriles[i].Value;
                BoundingBox box = new BoundingBox(trile.Position, trile.Position + Vector3.One);
                float? intersection = ray.Intersects(box);
                if (intersection.HasValue && !trilesGot.Contains(trile)) {
                    trilesGot.Add(trile);
                }
            }
            return trilesGot;
        }
        
        public List<TrileInstance> GetTrilesSelected(List<TrileInstance> selected = null) {
            float x = DragOrigin.X;
            float y = DragOrigin.Y;
            float w = MouseState.Position.X - DragOrigin.X;
            float h = MouseState.Position.Y - DragOrigin.Y;
            if (w < 0) {
                x += w;
                w = -w;
            }
            if (h < 0) {
                y += h;
                h = -h;
            }
            
            if (selected == null) {
                selected = new List<TrileInstance>();
            }
            
            for (float yy = y; yy < y + h; yy += SelectRayDistance) {
                for (float xx = x; xx < x + w; xx += SelectRayDistance) {
                    GetTriles(new Ray(
                        GraphicsDevice.Viewport.Unproject(
                            new Vector3(xx, yy, 0.0f),
                            CameraManager.Projection, CameraManager.View, Matrix.Identity
                        ),
                        CameraManager.InverseView.Forward
                    ), selected);
                }
            }
            
            return selected;
        }
        
        public Mesh GenMesh(Trile trile, float phi = 0f, BaseEffect effect = null) {
            Mesh mesh = new Mesh() {
                SamplerState = SamplerState.PointClamp,
                DepthWrites = true,
                Effect = effect
            };
            ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> siip = trile.Geometry;
            Group group = mesh.AddGroup();
            group.Geometry = new IndexedUserPrimitives<VertexPositionNormalTextureInstance>(siip.Vertices, siip.Indices, siip.PrimitiveType);
            group.Texture = LevelMaterializer.TrilesMesh.Texture;
            mesh.SetRotation(trile, phi);
            return mesh;
        }
        
        public Mesh GenMesh(TrileInstance trile, BaseEffect effect = null) {
            return GenMesh(trile.Trile, trile.Phi, effect);
        }
        
        public Mesh GenMesh(ArtObject ao, float phi = 0f, BaseEffect effect = null) {
            Mesh mesh = new Mesh() {
                SamplerState = SamplerState.PointClamp,
                DepthWrites = true,
                Effect = effect
            };
            ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix> siip = ao.Geometry;
            Group group = mesh.AddGroup();
            group.Geometry = new IndexedUserPrimitives<VertexPositionNormalTextureInstance>(siip.Vertices, siip.Indices, siip.PrimitiveType);
            group.Texture = ao.Cubemap;
            mesh.SetRotation(phi);
            return mesh;
        }
        
        public void AddTrile(TrileInstance trile) {
            if (LevelManager.TrileExists(trile.Emplacement)) {
                TrileEmplacement emplacement = trile.Emplacement;
                LevelMaterializer.RemoveInstance(LevelManager.TrileInstanceAt(ref emplacement));
            }

            if (LevelManager.Triles.ContainsKey(trile.Emplacement)) {
                //TODO investigate: Probably an empty trile.
                LevelManager.Triles.Remove(trile.Emplacement);
            }

            trile.Update();
            trile.OriginalEmplacement = trile.Emplacement;
            trile.RefreshTrile();

            LevelMaterializer.AddInstance(trile);
            LevelManager.Triles.Add(trile.Emplacement, trile);
            trile.Removed = false;

            if (LevelMaterializer.GetTrileMaterializer(trile.Trile) == null) {
                LevelMaterializer.RebuildTrile(trile.Trile);
                LevelMaterializer.RebuildInstances();
            }

            LevelManager.RecullAt(trile);

            trile.PhysicsState.UpdateInstance();
            LevelMaterializer.UpdateInstance(trile);


            if (LevelManager.Triles.Count == 1) {
                PlayerManager.CheckpointGround = trile;
                PlayerManager.RespawnAtCheckpoint();
            }

            CameraManager.RebuildView();
            if (CameraManager is DefaultCameraManager) {
                ((DefaultCameraManager) CameraManager).RebuildProjection();
            }
        }

        protected readonly static FaceOrientation[] faces = {
            FaceOrientation.Right,
            FaceOrientation.Left,
            FaceOrientation.Top,
            FaceOrientation.Down,
            FaceOrientation.Front,
            FaceOrientation.Back
        };
        protected FaceOrientation GetHoveredFace(BoundingBox box, Ray ray) {
            float intersectionMin = float.MaxValue;

            BoundingBox[] sides = EditorUtils.a_BoundingBox_6.GetNext();
            
            sides[0] = new BoundingBox(new Vector3(box.Min.X, box.Min.Y, box.Min.Z), new Vector3(box.Min.X, box.Max.Y, box.Max.Z));
            sides[1] = new BoundingBox(new Vector3(box.Max.X, box.Min.Y, box.Min.Z), new Vector3(box.Max.X, box.Max.Y, box.Max.Z));
            sides[2] = new BoundingBox(new Vector3(box.Min.X, box.Min.Y, box.Min.Z), new Vector3(box.Max.X, box.Min.Y, box.Max.Z));
            sides[3] = new BoundingBox(new Vector3(box.Min.X, box.Max.Y, box.Min.Z), new Vector3(box.Max.X, box.Max.Y, box.Max.Z));
            sides[4] = new BoundingBox(new Vector3(box.Min.X, box.Min.Y, box.Min.Z), new Vector3(box.Max.X, box.Max.Y, box.Min.Z));
            sides[5] = new BoundingBox(new Vector3(box.Min.X, box.Min.Y, box.Max.Z), new Vector3(box.Max.X, box.Max.Y, box.Max.Z));

            FaceOrientation face = FaceOrientation.Top;

            for (int i = 0; i < 6; i++) {
                float? intersection = ray.Intersects(sides[i]);
                if (intersection.HasValue && intersection < intersectionMin) {
                    intersectionMin = intersection.Value;
                    face = faces[i];
                }
            }

            return face;
        }

        public void Save(bool binary = true, bool overwrite = false) {
            WindowHeaderWidget windowHeader = null;

            string filePath_ = ("levels\\" + LevelManager.Name).Externalize() + ".";
            string fileExtension = (binary ? "fmb" : "xml");
            string filePath = filePath_ + fileExtension;
            FileInfo file = new FileInfo(filePath);
            file.Directory.Create();

            Action save = delegate() {
                if (file.Exists) {
                    FileInfo fileBackupOldest = new FileInfo(filePath_ + FezEditor.Settings.BackupHistory + "." + fileExtension);
                    if (fileBackupOldest.Exists) {
                        fileBackupOldest.Delete();
                    }

                    for (int i = FezEditor.Settings.BackupHistory - 1; i > 0; i--) {
                        FileInfo fileBackup = new FileInfo(filePath_ + i + "." + fileExtension);
                        if (fileBackup.Exists) {
                            fileBackup.MoveTo(filePath_ + (i+1) + "." + fileExtension);
                        }
                    }

                    if (FezEditor.Settings.BackupHistory <= 0) {
                        file.Delete();
                    } else {
                        file.MoveTo(filePath_ + "1." + fileExtension);
                    }
                }
                ModLogger.Log("FEZMod.Editor", "Saving level "+LevelManager.Name);
                GameLevelManagerHelper.Save(LevelManager.Name, binary);
                CreateThumbnail();
                if (windowHeader != null) {
                    windowHeader.CloseButtonWidget.Action();
                }
            };

            if (!file.Exists || overwrite) {
                save();
                return;
            }

            ContainerWidget window;
            Widgets.Add(window = new ContainerWidget(Game) {
                Size = new Vector2(256f, 48f),
                Label = "Overwrite level?"
            });
            window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
            window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

            window.Widgets.Add(new ButtonWidget(Game, "Level already existing. Overwrite?") {
                Background = new Color(DefaultBackground, 0f),
                Size = new Vector2(window.Size.X, 24f),
                UpdateBounds = false,
                LabelCentered = true,
                Position = new Vector2(0f, 0f)
            });

            window.Widgets.Add(new ButtonWidget(Game, "YES", save) {
                Size = new Vector2(window.Size.X / 2f, 24f),
                UpdateBounds = false,
                LabelCentered = true,
                Position = new Vector2(0f, 24f)
            });

            window.Widgets.Add(new ButtonWidget(Game, "NO", windowHeader.CloseButtonWidget.Action) {
                Size = new Vector2(window.Size.X / 2f, 24f),
                UpdateBounds = false,
                LabelCentered = true,
                Position = new Vector2(window.Size.X / 2f, 24f)
            });
        }

        public void CreateThumbnail(int size = 128, bool overwrite = true) {
            string filePath = ("other textures/map_screens/" + LevelManager.Name).Externalize() + ".png";
            if (File.Exists(filePath)) {
                if (!overwrite) {
                    return;
                }
                File.Delete(filePath);
            }
            ThumbnailSize = size;
            Widgets.Add(new ThumbnailCreatorWidget(Game));
        }

    }
}

