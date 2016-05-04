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
#if FNA
using Microsoft.Xna.Framework.Input;
using SDL2;
#endif

namespace FezGame.Components {
    public class LevelEditor : DrawableGameComponent, ILevelEditor, IGuiHandler {

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

        protected int SkipLoading = 0;

        protected KeyValuePair<TrileEmplacement, TrileInstance>[] tmpTriles = new KeyValuePair<TrileEmplacement, TrileInstance>[8192];
        protected KeyValuePair<int, ArtObjectInstance>[] tmpAOs = new KeyValuePair<int, ArtObjectInstance>[128];

        public TrileInstance HoveredTrile { get; set; }
        public ArtObjectInstance HoveredAO { get; set; }
        public BoundingBox HoveredBox { get; set; }
        public FaceOrientation HoveredFace { get; set; }
        public int TrileId { get; set; }

        public List<GuiWidget> Widgets { get; set; }
        public List<Action> Scheduled { get; set; }

        public InfoWidget InfoWidget;
        public TopBarWidget TopBarWidget;
        public AssetPickerWidget AssetPickerWidget;
        public List<AssetPickerWidget> AssetPickerWidgets = new List<AssetPickerWidget>();
        public ContainerWidget AssetPickerPickerWidget;
        public List<ButtonWidget> AssetPickerLabels = new List<ButtonWidget>();
        
        public ButtonWidget TooltipWidget;
        protected bool TooltipWidgetAdded = false;

        protected GuiWidget DraggingWidget;
        protected GuiWidget FocusedWidget;

        public bool ThumbnailScheduled { get; set; }
        public int ThumbnailX { get; set; }
        public int ThumbnailY { get; set; }
        public int ThumbnailSize { get; set; }
        protected RenderTargetHandle ThumbnailRT;

        public Color DefaultForeground {
            get {
                return FezEditor.Settings.DefaultForeground;
            }
            set {
                FezEditor.Settings.DefaultForeground = value;
            }
        }
        public Color DefaultBackground {
            get {
                return FezEditor.Settings.DefaultBackground;
            }
            set {
                FezEditor.Settings.DefaultBackground = value;
            }
        }

        public LevelEditor(Game game)
            : base(game) {
            UpdateOrder = -10;
            DrawOrder = 4000;
            TrileId = 0;
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

            Widgets = new List<GuiWidget>();

            ButtonWidget button;

            //TOP BAR
            Widgets.Add(TopBarWidget = new TopBarWidget(Game));

            TopBarWidget.Widgets.Add(button = new ButtonWidget(Game, "File"));
            button.Background.A = 0;

            button.Widgets.Add(new ButtonWidget(Game, "New", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    Size = new Vector2(256f, 144f),
                    Label = "New level"
                });
                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
                WindowHeaderWidget windowHeader;
                window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

                ButtonWidget windowLabelName;
                window.Widgets.Add(windowLabelName = new ButtonWidget(Game, "Name:") {
                    Background = new Color(DefaultBackground, 0f),
                    Size = new Vector2(96f, 24f),
                    UpdateBounds = false,
                    LabelCentered = false,
                    Position = new Vector2(0f, 0f)
                });
                TextFieldWidget windowFieldName;
                window.Widgets.Add(windowFieldName = new TextFieldWidget(Game) {
                    Size = new Vector2(window.Size.X - windowLabelName.Size.X, 24f),
                    UpdateBounds = false,
                    Position = new Vector2(windowLabelName.Size.X, windowLabelName.Position.Y)
                });
                windowFieldName.Fill(ContentPaths.Levels);

                ButtonWidget windowLabelWidth;
                window.Widgets.Add(windowLabelWidth = new ButtonWidget(Game, "Width:") {
                    Background = new Color(DefaultBackground, 0f),
                    Size = new Vector2(96f, 24f),
                    UpdateBounds = false,
                    LabelCentered = false,
                    Position = new Vector2(0f, 24f)
                });
                TextFieldWidget windowFieldWidth;
                window.Widgets.Add(windowFieldWidth = new TextFieldWidget(Game) {
                    Size = new Vector2(window.Size.X - windowLabelWidth.Size.X, 24f),
                    UpdateBounds = false,
                    Position = new Vector2(windowLabelWidth.Size.X, windowLabelWidth.Position.Y)
                });

                ButtonWidget windowLabelHeight;
                window.Widgets.Add(windowLabelHeight = new ButtonWidget(Game, "Height:") {
                    Background = new Color(DefaultBackground, 0f),
                    Size = new Vector2(96f, 24f),
                    UpdateBounds = false,
                    LabelCentered = false,
                    Position = new Vector2(0f, 48f)
                });
                TextFieldWidget windowFieldHeight;
                window.Widgets.Add(windowFieldHeight = new TextFieldWidget(Game) {
                    Size = new Vector2(window.Size.X - windowLabelHeight.Size.X, 24f),
                    UpdateBounds = false,
                    Position = new Vector2(windowLabelHeight.Size.X, windowLabelHeight.Position.Y)
                });

                ButtonWidget windowLabelDepth;
                window.Widgets.Add(windowLabelDepth = new ButtonWidget(Game, "Depth:") {
                    Background = new Color(DefaultBackground, 0f),
                    Size = new Vector2(96f, 24f),
                    UpdateBounds = false,
                    LabelCentered = false,
                    Position = new Vector2(0f, 72f)
                });
                TextFieldWidget windowFieldDepth;
                window.Widgets.Add(windowFieldDepth = new TextFieldWidget(Game) {
                    Size = new Vector2(window.Size.X - windowLabelDepth.Size.X, 24f),
                    UpdateBounds = false,
                    Position = new Vector2(windowLabelDepth.Size.X, windowLabelDepth.Position.Y)
                });

                ButtonWidget windowLabelTrileset;
                window.Widgets.Add(windowLabelTrileset = new ButtonWidget(Game, "Trileset:") {
                    Background = new Color(DefaultBackground, 0f),
                    Size = new Vector2(96f, 24f),
                    UpdateBounds = false,
                    LabelCentered = false,
                    Position = new Vector2(0f, 96f)
                });
                TextFieldWidget windowFieldTrileset;
                window.Widgets.Add(windowFieldTrileset = new TextFieldWidget(Game) {
                    Size = new Vector2(window.Size.X - windowLabelTrileset.Size.X, 24f),
                    UpdateBounds = false,
                    Position = new Vector2(windowLabelTrileset.Size.X, windowLabelTrileset.Position.Y)
                });
                windowFieldTrileset.Fill(ContentPaths.TrileSets);

                window.Widgets.Add(new ButtonWidget(Game, "CREATE", delegate() {
                    Level level = CreateNewLevel(
                        windowFieldName.Text,
                        int.Parse(windowFieldWidth.Text),
                        int.Parse(windowFieldHeight.Text),
                        int.Parse(windowFieldDepth.Text),
                        windowFieldTrileset.Text
                    );
                    GameState.Loading = true;
                    SkipLoading = 8;
                    GameLevelManagerHelper.ChangeLevel(level);
                    windowHeader.CloseButtonWidget.Action();
                }) {
                    Size = new Vector2(window.Size.X, 24f),
                    UpdateBounds = false,
                    LabelCentered = true,
                    Position = new Vector2(0f, window.Size.Y - 24f)
                });
            }));

            TextFieldWidget fieldOpen;
            button.Widgets.Add(new ButtonWidget(Game, "Open", new GuiWidget[] {
                fieldOpen = new TextFieldWidget(Game, "", "Levels") {
                    Size = new Vector2(160f, 24f),
                    Position = new Vector2(0f, 0f)
                },
                new ButtonWidget(Game, "LOAD", delegate() {
                    GameState.Loading = true;
                    SkipLoading = 8;
                    LevelManager.ChangeLevel(fieldOpen.Text);
                }) {
                    LabelCentered = true
                }
            }));

            button.Widgets.Add(new ButtonWidget(Game, "Recreate thumbnail", new GuiWidget[] {
                new ButtonWidget(Game, "128px (default)", () => CreateThumbnail(128)),
                new ButtonWidget(Game, "256px", () => CreateThumbnail(256)),
                new ButtonWidget(Game, "512px", () => CreateThumbnail(512))
            }));
            /*button.Widgets.Add(new ButtonWidget(Game, "Save (XML)", () => Save()));
            button.Widgets.Add(new ButtonWidget(Game, "Save (binary)", () => Save(true)));*/
            button.Widgets.Add(new ButtonWidget(Game, "Save", () => Save(true)));

            TopBarWidget.Widgets.Add(button = new ButtonWidget(Game, "View"));
            button.Background.A = 0;

            button.Widgets.Add(new ButtonWidget(Game, "Perspective", new GuiWidget[] {
                new ButtonWidget(Game, "Front", () => CameraManager.ChangeViewpoint(Viewpoint.Front)),
                new ButtonWidget(Game, "Left", () => CameraManager.ChangeViewpoint(Viewpoint.Left)),
                new ButtonWidget(Game, "Back", () => CameraManager.ChangeViewpoint(Viewpoint.Back)),
                new ButtonWidget(Game, "Right", () => CameraManager.ChangeViewpoint(Viewpoint.Right)),
                new ButtonWidget(Game, "Perspective", () => CameraManager.ChangeViewpoint(Viewpoint.Perspective))
            }));

            TextFieldWidget fieldPPT;
            button.Widgets.Add(new ButtonWidget(Game, "Pixels per Trixel", new GuiWidget[] {
                fieldPPT = new TextFieldWidget(Game, "", new string[] {
                    "0.25",
                    "1",
                    "2",
                    "3",
                    "4"
                }) {
                    Size = new Vector2(160f, 24f),
                    Position = new Vector2(0f, 0f)
                },
                new ButtonWidget(Game, "CHANGE", delegate() {
                    CameraManager.PixelsPerTrixel = float.Parse(fieldPPT.Text);
                }) {
                    LabelCentered = true
                }
            }));

            TopBarWidget.Widgets.Add(button = new ButtonWidget(Game, "Level"));
            button.Background.A = 0;
            TextFieldWidget fieldName;
            TextFieldWidget fieldWidth;
            TextFieldWidget fieldHeight;
            TextFieldWidget fieldDepth;
            button.Widgets.Add(new ButtonWidget(Game, "Settings", new GuiWidget[] {
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Name:") {
                        Background = new Color(DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldName = new TextFieldWidget(Game) {
                        RefreshValue = () => LevelManager.Name,
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Width:") {
                        Background = new Color(DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldWidth = new TextFieldWidget(Game) {
                        RefreshValue = () => ((int) LevelManager.Size.X).ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Height:") {
                        Background = new Color(DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldHeight = new TextFieldWidget(Game) {
                        RefreshValue = () => ((int) LevelManager.Size.Y).ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Depth:") {
                        Background = new Color(DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldDepth = new TextFieldWidget(Game) {
                        RefreshValue = () => ((int) LevelManager.Size.Z).ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ButtonWidget(Game, "CHANGE", delegate() {
                    GameLevelManagerHelper.Level.Name = fieldName.Text;
                    GameLevelManagerHelper.Level.Size = new Vector3(
                        int.Parse(fieldWidth.Text),
                        int.Parse(fieldHeight.Text),
                        int.Parse(fieldDepth.Text)
                    );
                }) {
                    LabelCentered = true
                }
            }));

            TextFieldWidget fieldSpawnX;
            TextFieldWidget fieldSpawnY;
            TextFieldWidget fieldSpawnZ;
            TextFieldWidget fieldSpawnFace;
            button.Widgets.Add(new ButtonWidget(Game, "Spawnpoint", new GuiWidget[] {
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "X:") {
                        Background = new Color(DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldSpawnX = new TextFieldWidget(Game) {
                        RefreshValue = () => LevelManager.StartingPosition == null ? "0" : LevelManager.StartingPosition.Id.X.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Y:") {
                        Background = new Color(DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldSpawnY = new TextFieldWidget(Game) {
                        RefreshValue = () => LevelManager.StartingPosition == null ? "0" : LevelManager.StartingPosition.Id.Y.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Z:") {
                        Background = new Color(DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldSpawnZ = new TextFieldWidget(Game) {
                        RefreshValue = () => LevelManager.StartingPosition == null ? "0" : LevelManager.StartingPosition.Id.Z.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Face:") {
                        Background = new Color(DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldSpawnFace = new TextFieldWidget(Game, "", Enum.GetNames(typeof(FaceOrientation))) {
                        RefreshValue = () => LevelManager.StartingPosition == null ? "Front" : LevelManager.StartingPosition.Face.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ButtonWidget(Game, "CHANGE", delegate() {
                    if (LevelManager.StartingPosition == null) {
                        LevelManager.StartingPosition = new TrileFace();
                    }
                    LevelManager.StartingPosition.Id.X = int.Parse(fieldSpawnX.Text);
                    LevelManager.StartingPosition.Id.Y = int.Parse(fieldSpawnY.Text);
                    LevelManager.StartingPosition.Id.Z = int.Parse(fieldSpawnZ.Text);
                    LevelManager.StartingPosition.Face = (FaceOrientation) Enum.Parse(typeof(FaceOrientation), fieldSpawnFace.Text);
                }) {
                    LabelCentered = true
                }
            }));

            TextFieldWidget fieldSky;
            button.Widgets.Add(new ButtonWidget(Game, "Sky", new GuiWidget[] {
                fieldSky = new TextFieldWidget(Game, "", ContentPaths.Skies) {
                    RefreshValue = () => (LevelManager.Sky != null) ? LevelManager.Sky.Name : "",
                    Size = new Vector2(160f, 24f),
                    Position = new Vector2(0f, 0f)
                },
                new ButtonWidget(Game, "CHANGE", delegate() {
                    Sky sky = fieldSky.Text.Length > 0 ? CMProvider.CurrentLevel.Load<Sky>("Skies/" + fieldSky.Text) : null;
                    if (sky != null) {
                        GameLevelManagerHelper.Level.Sky = sky;
                        GameLevelManagerHelper.Level.SkyName = sky.Name;
                        LevelManager.ChangeSky(sky);
                    }
                }) {
                    LabelCentered = true
                }
            }));

            TextFieldWidget fieldSong;
            button.Widgets.Add(new ButtonWidget(Game, "Song", new GuiWidget[] {
                fieldSong = new TextFieldWidget(Game, "", ContentPaths.Music) {
                    RefreshValue = () => (LevelManager.Song != null) ? LevelManager.SongName : "",
                    Size = new Vector2(160f, 24f),
                    Position = new Vector2(0f, 0f)
                },
                new ButtonWidget(Game, "CHANGE", delegate() {
                    TrackedSong song = fieldSong.Text.Length > 0 ? CMProvider.CurrentLevel.Load<TrackedSong>("Music/" + fieldSong.Text) : null;
                    if (song != null) {
                        song.Initialize();
                        GameLevelManagerHelper.Level.Song = song;
                        LevelManager.SongChanged = true;
                        SoundManager.PlayNewSong(song.Name);
                        SoundManager.UpdateSongActiveTracks();
                    }
                    GameLevelManagerHelper.Level.SongName = song != null ? song.Name : null;
                }) {
                    LabelCentered = true
                }
            }));

            TextFieldWidget fieldWaterHeight;
            TextFieldWidget fieldWaterType;
            button.Widgets.Add(new ButtonWidget(Game, "Water", new GuiWidget[] {
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Type:") {
                        Background = new Color(DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldWaterType = new TextFieldWidget(Game, "", Enum.GetNames(typeof(LiquidType))) {
                        RefreshValue = () => LevelManager.WaterType.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    },
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Height:") {
                        Background = new Color(DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldWaterHeight = new TextFieldWidget(Game, "") {
                        RefreshValue = () => LevelManager.WaterHeight.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    },
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ButtonWidget(Game, "CHANGE", delegate() {
                    LevelManager.WaterType = (LiquidType) Enum.Parse(typeof(LiquidType), fieldWaterType.Text);
                    LevelManager.WaterHeight = float.Parse(fieldWaterHeight.Text);
                }) {
                    LabelCentered = true
                }
            }));


            TopBarWidget.Widgets.Add(button = new ButtonWidget(Game, "Scripting"));
            button.Background.A = 0;

            button.Widgets.Add(new ButtonWidget(Game, "Volumes", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true,
                    Size = new Vector2(512f, 24f),
                    Label = "Volumes"
                });

                window.RefreshValue = delegate() {
                    window.Widgets.Clear();
                    window.Widgets.Add(new WindowHeaderWidget(Game));

                    int i = 0;
                    foreach (Volume volume in LevelManager.Volumes.Values) {
                        window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                            new ButtonWidget(Game, "["+volume.Id+"] "+EditorUtils.ToString(volume.From)+" - "+EditorUtils.ToString(volume.To)) {
                                Size = new Vector2(window.Size.X - 24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = false,
                                Position = new Vector2(0f, 0f)
                            },
                            new ButtonWidget(Game, "X", delegate() {
                                LevelManager.Volumes.Remove(volume.Id);
                                window.Refresh();
                            }) {
                                Background = new Color(0.5f, 0f, 0f, 1f),
                                Size = new Vector2(24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = true,
                                Position = new Vector2(window.Size.X - 24f, 0f)
                            }
                        }) {
                            Size = new Vector2(window.Size.X, 24f),
                            Background = new Color(DefaultBackground, 0f)
                        });

                        i++;
                    }

                    window.Size.Y = (i+1) * 24f;
                    window.Size.Y = Math.Min(512f, window.Size.Y);

                    window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                        ContainerWidget windowAdd;
                        Widgets.Add(windowAdd = new ContainerWidget(Game) {
                            Size = new Vector2(256f, 192f),
                            Label = "Add Volume"
                        });
                        WindowHeaderWidget windowAddHeader;
                        windowAdd.Widgets.Add(windowAddHeader = new WindowHeaderWidget(Game));

                        ButtonWidget windowLabelId;
                        windowAdd.Widgets.Add(windowLabelId = new ButtonWidget(Game, "ID:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        });
                        TextFieldWidget windowFieldId;
                        windowAdd.Widgets.Add(windowFieldId = new TextFieldWidget(Game) {
                            Size = new Vector2(windowAdd.Size.X - windowLabelId.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelId.Size.X, windowLabelId.Position.Y)
                        });

                        ButtonWidget windowLabelFrom;
                        windowAdd.Widgets.Add(windowLabelFrom = new ButtonWidget(Game, "From:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 24f)
                        });
                        TextFieldWidget windowFieldFrom;
                        windowAdd.Widgets.Add(windowFieldFrom = new TextFieldWidget(Game, "0; 0; 0") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelFrom.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelFrom.Size.X, windowLabelFrom.Position.Y)
                        });

                        ButtonWidget windowLabelTo;
                        windowAdd.Widgets.Add(windowLabelTo = new ButtonWidget(Game, "To:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 48f)
                        });
                        TextFieldWidget windowFieldTo;
                        windowAdd.Widgets.Add(windowFieldTo = new TextFieldWidget(Game, "1; 1; 1") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelTo.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelTo.Size.X, windowLabelTo.Position.Y)
                        });

                        CheckboxWidget windowCheckFront;
                        windowAdd.Widgets.Add(windowCheckFront = new CheckboxWidget(Game, "Front") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 72f)
                        });

                        CheckboxWidget windowCheckLeft;
                        windowAdd.Widgets.Add(windowCheckLeft = new CheckboxWidget(Game, "Left") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 96f)
                        });

                        CheckboxWidget windowCheckBack;
                        windowAdd.Widgets.Add(windowCheckBack = new CheckboxWidget(Game, "Back") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 120f)
                        });

                        CheckboxWidget windowCheckRight;
                        windowAdd.Widgets.Add(windowCheckRight = new CheckboxWidget(Game, "Right") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 144f)
                        });

                        windowAdd.Widgets.Add(new ButtonWidget(Game, "ADD", delegate() {
                            string[] fromSplit = windowFieldFrom.Text.Split(new char[] {';'});
                            string[] toSplit = windowFieldTo.Text.Split(new char[] {';'});
                            Volume volume = new Volume() {
                                Id = int.Parse(windowFieldId.Text),
                                From = new Vector3(
                                    float.Parse(fromSplit[0].Trim()),
                                    float.Parse(fromSplit[1].Trim()),
                                    float.Parse(fromSplit[2].Trim())
                                ),
                                To = new Vector3(
                                    float.Parse(toSplit[0].Trim()),
                                    float.Parse(toSplit[1].Trim()),
                                    float.Parse(toSplit[2].Trim())
                                )
                            };
                            if (windowCheckFront.Checked) {volume.Orientations.Add(FaceOrientation.Front);}
                            if (windowCheckLeft.Checked) {volume.Orientations.Add(FaceOrientation.Left);}
                            if (windowCheckBack.Checked) {volume.Orientations.Add(FaceOrientation.Back);}
                            if (windowCheckRight.Checked) {volume.Orientations.Add(FaceOrientation.Right);}
                            LevelManager.Volumes.Add(volume.Id, volume);
                            windowAddHeader.CloseButtonWidget.Action();
                            window.Refresh();
                        }) {
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(0f, windowAdd.Size.Y - 24f)
                        });

                        windowAdd.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (windowAdd.Size.X / 2);
                        windowAdd.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (windowAdd.Size.Y / 2);
                    }) {
                        Background = new Color(0f, 0.125f, 0f, 1f),
                        Size = new Vector2(window.Size.X, 24f),
                        UpdateBounds = false,
                        LabelCentered = true,
                        Position = new Vector2(0f, window.Size.Y - 24f)
                    });

                    return null;
                };
                window.Refresh();

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));

            button.Widgets.Add(new ButtonWidget(Game, "Scripts", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true
                });
                window.Size.X = 512f;
                window.Size.Y = 24f;
                window.Label = "Scripts";
                window.Widgets.Add(new WindowHeaderWidget(Game));

                int i = 0;
                foreach (Script script in LevelManager.Scripts.Values) {
                    window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                        new ButtonWidget(Game, "["+script.Id+"] "+script.Name+" ("+(script.Triggerless ? "none" : (script.Triggers.Count == 1 ? script.Triggers[0].ToString() : "..."))+") : "+(script.Actions.Count == 1 ? script.Actions[0].ToString() : "...")) {
                            Size = new Vector2(window.Size.X - 48f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        },
                        new ButtonWidget(Game, "C") {
                            Background = new Color(0f, 0f, 0.125f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 48f, 0f)
                        },
                        new ButtonWidget(Game, "X") {
                            Background = new Color(0.5f, 0f, 0f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 24f, 0f)
                        }
                    }) {
                        Size = new Vector2(window.Size.X, 24f),
                        Background = new Color(DefaultBackground, 0f)
                    });

                    i++;
                }

                window.Size.Y += i * 24f;
                window.Size.Y = Math.Min(512f, window.Size.Y);

                window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                }) {
                    Background = new Color(0f, 0.125f, 0f, 1f),
                    Size = new Vector2(window.Size.X, 24f),
                    UpdateBounds = false,
                    LabelCentered = true,
                    Position = new Vector2(0f, window.Size.Y - 24f)
                });

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Art Objects", delegate() {
                ContainerWidget window;

                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true,
                    Size = new Vector2(512f, 24f),
                    Label = "Art Objects"
                });

                window.RefreshValue = delegate() {
                    window.Widgets.Clear();

                    window.Widgets.Add(new WindowHeaderWidget(Game));

                    int i = 0;
                    foreach (ArtObjectInstance ao in LevelManager.ArtObjects.Values) {
                        window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                            new ButtonWidget(Game, "["+ao.Id+"] "+ao.ArtObjectName+": "+EditorUtils.ToString(ao.Position)) {
                                Size = new Vector2(window.Size.X - 24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = false,
                                Position = new Vector2(0f, 0f)
                            },
                            new ButtonWidget(Game, "X", delegate() {
                                int trileGroupId = ao.ActorSettings.AttachedGroup.HasValue ? ao.ActorSettings.AttachedGroup.Value : -1;
                                if (LevelManager.Groups.ContainsKey(trileGroupId)) {
                                    TrileGroup trileGroup = LevelManager.Groups[trileGroupId];
                                    while (trileGroup.Triles.Count > 0) {
                                        LevelManager.ClearTrile(trileGroup.Triles[0]);
                                    }
                                    LevelManager.Groups.Remove(trileGroupId);
                                }
                                LevelManager.ArtObjects.Remove(ao.Id);
                                ao.Dispose();
                                LevelMaterializer.RegisterSatellites();

                                window.Refresh();
                            }) {
                                Background = new Color(0.5f, 0f, 0f, 1f),
                                Size = new Vector2(24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = true,
                                Position = new Vector2(window.Size.X - 24f, 0f)
                            }
                        }) {
                            Size = new Vector2(window.Size.X, 24f),
                            Background = new Color(DefaultBackground, 0f)
                        });

                        i++;
                    }

                    window.Size.Y = (i+1) * 24f;
                    window.Size.Y = Math.Min(512f, window.Size.Y);

                    window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                        ContainerWidget windowAdd;
                        Widgets.Add(windowAdd = new ContainerWidget(Game) {
                            Size = new Vector2(256f, 168f),
                            Label = "Add Art Object"
                        });
                        WindowHeaderWidget windowAddHeader;
                        windowAdd.Widgets.Add(windowAddHeader = new WindowHeaderWidget(Game));

                        int maxID = 0;
                        foreach (int id in LevelManager.ArtObjects.Keys) {
                            if (id >= maxID) {
                                maxID = id + 1;
                            }
                        }

                        ButtonWidget windowLabelId;
                        windowAdd.Widgets.Add(windowLabelId = new ButtonWidget(Game, "ID:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        });
                        TextFieldWidget windowFieldId;
                        windowAdd.Widgets.Add(windowFieldId = new TextFieldWidget(Game, maxID.ToString()) {
                            Size = new Vector2(windowAdd.Size.X - windowLabelId.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelId.Size.X, windowLabelId.Position.Y)
                        });

                        ButtonWidget windowLabelName;
                        windowAdd.Widgets.Add(windowLabelName = new ButtonWidget(Game, "Name:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 24f)
                        });
                        TextFieldWidget windowFieldName;
                        windowAdd.Widgets.Add(windowFieldName = new TextFieldWidget(Game, "", ContentPaths.ArtObjects) {
                            Size = new Vector2(windowAdd.Size.X - windowLabelName.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelName.Size.X, windowLabelName.Position.Y)
                        });

                        ButtonWidget windowLabelPosition;
                        windowAdd.Widgets.Add(windowLabelPosition = new ButtonWidget(Game, "Position:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 48f)
                        });
                        TextFieldWidget windowFieldPosition;
                        windowAdd.Widgets.Add(windowFieldPosition = new TextFieldWidget(Game, "0; 0; 0") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelPosition.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelPosition.Size.X, windowLabelPosition.Position.Y)
                        });

                        ButtonWidget windowLabelRotation;
                        windowAdd.Widgets.Add(windowLabelRotation = new ButtonWidget(Game, "Rotation:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 72f)
                        });
                        TextFieldWidget windowFieldRotation;
                        windowAdd.Widgets.Add(windowFieldRotation = new TextFieldWidget(Game, "0; 0; 0; 1") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelRotation.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelRotation.Size.X, windowLabelRotation.Position.Y)
                        });

                        ButtonWidget windowLabelCenter;
                        windowAdd.Widgets.Add(windowLabelCenter = new ButtonWidget(Game, "Center:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 96f)
                        });
                        TextFieldWidget windowFieldCenter;
                        windowAdd.Widgets.Add(windowFieldCenter = new TextFieldWidget(Game, "0; 0; 0") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelCenter.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelCenter.Size.X, windowLabelCenter.Position.Y)
                        });

                        ButtonWidget windowLabelScale;
                        windowAdd.Widgets.Add(windowLabelScale = new ButtonWidget(Game, "Scale:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 120f)
                        });
                        TextFieldWidget windowFieldScale;
                        windowAdd.Widgets.Add(windowFieldScale = new TextFieldWidget(Game, "1; 1; 1") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelScale.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelScale.Size.X, windowLabelScale.Position.Y)
                        });

                        windowAdd.Widgets.Add(new ButtonWidget(Game, "ADD", delegate() {
                            string[] positionSplit = windowFieldPosition.Text.Split(new char[] {';'});
                            string[] rotationSplit = windowFieldRotation.Text.Split(new char[] {';'});
                            string[] centerSplit = windowFieldCenter.Text.Split(new char[] {';'});
                            string[] scaleSplit = windowFieldScale.Text.Split(new char[] {';'});
                            ArtObjectInstance ao = new ArtObjectInstance(windowFieldName.Text) {
                                Id = int.Parse(windowFieldId.Text),
                                Position = new Vector3(
                                    float.Parse(positionSplit[0].Trim()),
                                    float.Parse(positionSplit[1].Trim()),
                                    float.Parse(positionSplit[2].Trim())
                                ),
                                Rotation = new Quaternion(
                                    float.Parse(rotationSplit[0].Trim()),
                                    float.Parse(rotationSplit[1].Trim()),
                                    float.Parse(rotationSplit[2].Trim()),
                                    float.Parse(rotationSplit[3].Trim())
                                ),
                                Scale = new Vector3(
                                    float.Parse(scaleSplit[0].Trim()),
                                    float.Parse(scaleSplit[1].Trim()),
                                    float.Parse(scaleSplit[2].Trim())
                                )
                            };
                            ao.ActorSettings = new ArtObjectActorSettings() {
                                RotationCenter = new Vector3(
                                    float.Parse(centerSplit[0].Trim()),
                                    float.Parse(centerSplit[1].Trim()),
                                    float.Parse(centerSplit[2].Trim())
                                )
                            };
                            ao.ArtObject = CMProvider.CurrentLevel.Load<ArtObject>("Art objects/"+ao.ArtObjectName);
                            ao.Initialize();
                            LevelManager.ArtObjects[ao.Id] = ao;
                            LevelMaterializer.RegisterSatellites();

                            windowAddHeader.CloseButtonWidget.Action();
                            window.Refresh();
                        }) {
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(0f, windowAdd.Size.Y - 24f)
                        });

                        windowAdd.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (windowAdd.Size.X / 2);
                        windowAdd.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (windowAdd.Size.Y / 2);
                    }) {
                        Background = new Color(0f, 0.125f, 0f, 1f),
                        Size = new Vector2(window.Size.X, 24f),
                        UpdateBounds = false,
                        LabelCentered = true,
                        Position = new Vector2(0f, window.Size.Y - 24f)
                    });

                    return null;
                };

                window.Refresh();

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Background Planes", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true,
                    Size = new Vector2(512f, 24f),
                    Label = "Background Planes"
                });

                window.RefreshValue = delegate() {
                    window.Widgets.Clear();

                    window.Widgets.Add(new WindowHeaderWidget(Game));

                    int i = 0;
                    foreach (BackgroundPlane bp in LevelManager.BackgroundPlanes.Values) {
                        window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                            new ButtonWidget(Game, "["+bp.Id+"] "+bp.TextureName+": "+EditorUtils.ToString(bp.Position)) {
                                Size = new Vector2(window.Size.X - 24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = false,
                                Position = new Vector2(0f, 0f)
                            },
                            new ButtonWidget(Game, "X", delegate() {
                                LevelManager.RemovePlane(bp);
                                window.Refresh();
                            }) {
                                Background = new Color(0.5f, 0f, 0f, 1f),
                                Size = new Vector2(24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = true,
                                Position = new Vector2(window.Size.X - 24f, 0f)
                            }
                        }) {
                            Size = new Vector2(window.Size.X, 24f),
                            Background = new Color(DefaultBackground, 0f)
                        });

                        i++;
                    }

                    window.Size.Y = (i+1) * 24f;
                    window.Size.Y = Math.Min(512f, window.Size.Y);

                    window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                        ContainerWidget windowAdd;
                        Widgets.Add(windowAdd = new ContainerWidget(Game) {
                            Size = new Vector2(256f, 240f),
                            Label = "Add Background Plane"
                        });
                        WindowHeaderWidget windowAddHeader;
                        windowAdd.Widgets.Add(windowAddHeader = new WindowHeaderWidget(Game));

                        int maxID = 0;
                        foreach (int id in LevelManager.BackgroundPlanes.Keys) {
                            if (id >= maxID) {
                                maxID = id + 1;
                            }
                        }

                        ButtonWidget windowLabelId;
                        windowAdd.Widgets.Add(windowLabelId = new ButtonWidget(Game, "ID:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        });
                        TextFieldWidget windowFieldId;
                        windowAdd.Widgets.Add(windowFieldId = new TextFieldWidget(Game, maxID.ToString()) {
                            Size = new Vector2(windowAdd.Size.X - windowLabelId.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelId.Size.X, windowLabelId.Position.Y)
                        });

                        ButtonWidget windowLabelName;
                        windowAdd.Widgets.Add(windowLabelName = new ButtonWidget(Game, "Name:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 24f)
                        });
                        TextFieldWidget windowFieldName;
                        windowAdd.Widgets.Add(windowFieldName = new TextFieldWidget(Game, "", ContentPaths.BackgroundPlanes) {
                            Size = new Vector2(windowAdd.Size.X - windowLabelName.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelName.Size.X, windowLabelName.Position.Y)
                        });

                        ButtonWidget windowLabelPosition;
                        windowAdd.Widgets.Add(windowLabelPosition = new ButtonWidget(Game, "Position:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 48f)
                        });
                        TextFieldWidget windowFieldPosition;
                        windowAdd.Widgets.Add(windowFieldPosition = new TextFieldWidget(Game, "0; 0; 0") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelPosition.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelPosition.Size.X, windowLabelPosition.Position.Y)
                        });

                        ButtonWidget windowLabelRotation;
                        windowAdd.Widgets.Add(windowLabelRotation = new ButtonWidget(Game, "Rotation:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 72f)
                        });
                        TextFieldWidget windowFieldRotation;
                        windowAdd.Widgets.Add(windowFieldRotation = new TextFieldWidget(Game, "0; 0; 0; 1") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelRotation.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelRotation.Size.X, windowLabelRotation.Position.Y)
                        });

                        ButtonWidget windowLabelScale;
                        windowAdd.Widgets.Add(windowLabelScale = new ButtonWidget(Game, "Scale:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 96f)
                        });
                        TextFieldWidget windowFieldScale;
                        windowAdd.Widgets.Add(windowFieldScale = new TextFieldWidget(Game, "1; 1; 1") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelScale.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelScale.Size.X, windowLabelScale.Position.Y)
                        });

                        ButtonWidget windowLabelFilter;
                        windowAdd.Widgets.Add(windowLabelFilter = new ButtonWidget(Game, "Filter:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 120f)
                        });
                        TextFieldWidget windowFieldFilter;
                        windowAdd.Widgets.Add(windowFieldFilter = new TextFieldWidget(Game, "#FFFFFFFF") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelFilter.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelFilter.Size.X, windowLabelFilter.Position.Y)
                        });

                        CheckboxWidget windowCheckboxBillboard;
                        windowAdd.Widgets.Add(windowCheckboxBillboard = new CheckboxWidget(Game, "Billboard") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 144f)
                        });

                        CheckboxWidget windowCheckboxLightMap;
                        windowAdd.Widgets.Add(windowCheckboxLightMap = new CheckboxWidget(Game, "Lightmap") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 168f)
                        });

                        CheckboxWidget windowCheckboxOverbright;
                        windowAdd.Widgets.Add(windowCheckboxOverbright = new CheckboxWidget(Game, "Overbright") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 192f)
                        });

                        windowAdd.Widgets.Add(new ButtonWidget(Game, "ADD", delegate() {
                            string[] positionSplit = windowFieldPosition.Text.Split(new char[] {';'});
                            string[] rotationSplit = windowFieldRotation.Text.Split(new char[] {';'});
                            string[] scaleSplit = windowFieldScale.Text.Split(new char[] {';'});
                            BackgroundPlane plane = new BackgroundPlane() {
                                Id = int.Parse(windowFieldId.Text),
                                TextureName = windowFieldName.Text,
                                Position = new Vector3(
                                    float.Parse(positionSplit[0].Trim()),
                                    float.Parse(positionSplit[1].Trim()),
                                    float.Parse(positionSplit[2].Trim())
                                ),
                                Rotation = new Quaternion(
                                    float.Parse(rotationSplit[0].Trim()),
                                    float.Parse(rotationSplit[1].Trim()),
                                    float.Parse(rotationSplit[2].Trim()),
                                    float.Parse(rotationSplit[3].Trim())
                                ),
                                Scale = new Vector3(
                                    float.Parse(scaleSplit[0].Trim()),
                                    float.Parse(scaleSplit[1].Trim()),
                                    float.Parse(scaleSplit[2].Trim())
                                ),
                                Billboard = windowCheckboxBillboard.Checked,
                                LightMap = windowCheckboxLightMap.Checked,
                                AllowOverbrightness = windowCheckboxOverbright.Checked
                            };
                            Color filter = new Color();
                            filter.PackedValue = Convert.ToUInt32(windowFieldFilter.Text.Substring(1), 16);
                            plane.Filter = filter;
                            plane.Animated = CMProvider.CurrentLevel.Load<object>("Background Planes/" + plane.TextureName) is AnimatedTexture;
                            plane.HostMesh = ((!plane.Animated) ? LevelMaterializer.StaticPlanesMesh : LevelMaterializer.AnimatedPlanesMesh);
                            plane.Initialize();
                            LevelManager.AddPlane(plane);

                            windowAddHeader.CloseButtonWidget.Action();
                            window.Refresh();
                        }) {
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(0f, windowAdd.Size.Y - 24f)
                        });

                        windowAdd.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (windowAdd.Size.X / 2);
                        windowAdd.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (windowAdd.Size.Y / 2);
                    }) {
                        Background = new Color(0f, 0.125f, 0f, 1f),
                        Size = new Vector2(window.Size.X, 24f),
                        UpdateBounds = false,
                        LabelCentered = true,
                        Position = new Vector2(0f, window.Size.Y - 24f)
                    });

                    return null;
                };

                window.Refresh();

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Groups", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true,
                    Size = new Vector2(256f, 24f),
                    Label = "Groups"
                });

                window.RefreshValue = delegate() {
                    window.Widgets.Clear();

                    window.Widgets.Add(new WindowHeaderWidget(Game));

                    int i = 0;
                    foreach (TrileGroup group_ in LevelManager.Groups.Values) {
                        window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                            new ButtonWidget(Game, "["+group_.Id+"] "+group_.Name) {
                                Size = new Vector2(window.Size.X - 24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = false,
                                Position = new Vector2(0f, 0f)
                            },
                            new ButtonWidget(Game, "X", delegate() {
                                while (group_.Triles.Count > 0) {
                                    LevelManager.ClearTrile(group_.Triles[0]);
                                }
                                LevelManager.Groups.Remove(group_.Id);
                                window.Refresh();
                            }) {
                                Background = new Color(0.5f, 0f, 0f, 1f),
                                Size = new Vector2(24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = true,
                                Position = new Vector2(window.Size.X - 24f, 0f)
                            }
                        }) {
                            Size = new Vector2(window.Size.X, 24f),
                            Background = new Color(DefaultBackground, 0f)
                        });

                        i++;
                    }

                    window.Size.Y = (i+1) * 24f;
                    window.Size.Y = Math.Min(512f, window.Size.Y);

                    window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                    }) {
                        Background = new Color(0f, 0.125f, 0f, 1f),
                        Size = new Vector2(window.Size.X, 24f),
                        UpdateBounds = false,
                        LabelCentered = true,
                        Position = new Vector2(0f, window.Size.Y - 24f)
                    });

                    return null;
                };

                window.Refresh();

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "NPCs", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true,
                    Size = new Vector2(256f, 24f),
                    Label = "NPCs"
                });

                window.RefreshValue = delegate() {
                    window.Widgets.Clear();

                    window.Widgets.Add(new WindowHeaderWidget(Game));

                    int i = 0;
                    foreach (NpcInstance npc in LevelManager.NonPlayerCharacters.Values) {
                        window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                            new ButtonWidget(Game, "["+npc.Id+"] "+npc.Name) {
                                Size = new Vector2(window.Size.X - 24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = false,
                                Position = new Vector2(0f, 0f)
                            },
                            new ButtonWidget(Game, "X", delegate() {
                                LevelManager.NonPlayerCharacters.Remove(npc.Id);
                                ServiceHelper.RemoveComponent(npc.State);
                                window.Refresh();
                            }) {
                                Background = new Color(0.5f, 0f, 0f, 1f),
                                Size = new Vector2(24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = true,
                                Position = new Vector2(window.Size.X - 24f, 0f)
                            }
                        }) {
                            Size = new Vector2(window.Size.X, 24f),
                            Background = new Color(DefaultBackground, 0f)
                        });

                        i++;
                    }

                    window.Size.Y = (i+1) * 24f;
                    window.Size.Y = Math.Min(512f, window.Size.Y);

                    window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                        ContainerWidget windowAdd;
                        Widgets.Add(windowAdd = new ContainerWidget(Game) {
                            Size = new Vector2(256f * 2f, 240f),
                            Label = "Add NPC",
                            UpdateBounds = false
                        });
                        WindowHeaderWidget windowAddHeader;
                        windowAdd.Widgets.Add(windowAddHeader = new WindowHeaderWidget(Game));

                        int maxID = 0;
                        foreach (int id in LevelManager.NonPlayerCharacters.Keys) {
                            if (id >= maxID) {
                                maxID = id + 1;
                            }
                        }

                        float columnWidth = 256f;
                        ContainerWidget[] columns = new ContainerWidget[2];
                        for (int ci = columns.Length - 1; ci >= 0; ci--) {
                            columns[ci] = new ContainerWidget(Game) {
                                Position = new Vector2(ci * columnWidth, 0),
                                Size = new Vector2(columnWidth, windowAdd.Size.Y),
                                UpdateBounds = ci != 0,
                                Background = new Color(DefaultBackground, 0f)
                            };
                            windowAdd.Widgets.Add(columns[ci]);
                        }

                        ButtonWidget windowLabelId;
                        columns[0].Widgets.Add(windowLabelId = new ButtonWidget(Game, "ID:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        });
                        TextFieldWidget windowFieldId;
                        columns[0].Widgets.Add(windowFieldId = new TextFieldWidget(Game, maxID.ToString()) {
                            Size = new Vector2(columnWidth - windowLabelId.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelId.Size.X, windowLabelId.Position.Y)
                        });

                        ButtonWidget windowLabelName;
                        columns[0].Widgets.Add(windowLabelName = new ButtonWidget(Game, "Name:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 24f)
                        });
                        TextFieldWidget windowFieldName;
                        columns[0].Widgets.Add(windowFieldName = new TextFieldWidget(Game, "", ContentPaths.CharacterAnimations) {
                            Size = new Vector2(columnWidth - windowLabelName.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelName.Size.X, windowLabelName.Position.Y)
                        });

                        ButtonWidget windowLabelPosition;
                        columns[0].Widgets.Add(windowLabelPosition = new ButtonWidget(Game, "Position:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 48f)
                        });
                        TextFieldWidget windowFieldPosition;
                        columns[0].Widgets.Add(windowFieldPosition = new TextFieldWidget(Game, "0; 0; 0") {
                            Size = new Vector2(columnWidth - windowLabelPosition.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelPosition.Size.X, windowLabelPosition.Position.Y)
                        });

                        ButtonWidget windowLabelDestination;
                        columns[0].Widgets.Add(windowLabelDestination = new ButtonWidget(Game, "Destination:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 72f)
                        });
                        TextFieldWidget windowFieldDestination;
                        columns[0].Widgets.Add(windowFieldDestination = new TextFieldWidget(Game, "0; 0; 0") {
                            Size = new Vector2(columnWidth - windowLabelDestination.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelDestination.Size.X, windowLabelDestination.Position.Y)
                        });

                        ButtonWidget windowLabelWalkSpeed;
                        columns[0].Widgets.Add(windowLabelWalkSpeed = new ButtonWidget(Game, "Walk speed:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 96f)
                        });
                        TextFieldWidget windowFieldWalkSpeed;
                        columns[0].Widgets.Add(windowFieldWalkSpeed = new TextFieldWidget(Game, "1") {
                            Size = new Vector2(columnWidth - windowLabelWalkSpeed.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelWalkSpeed.Size.X, windowLabelWalkSpeed.Position.Y)
                        });

                        CheckboxWidget windowCheckboxAvoidsGomez;
                        columns[0].Widgets.Add(windowCheckboxAvoidsGomez = new CheckboxWidget(Game, "Avoids Gomez") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(columnWidth, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 120f)
                        });

                        CheckboxWidget windowCheckboxOverrideTalk;
                        columns[1].Widgets.Add(windowCheckboxOverrideTalk = new CheckboxWidget(Game, "Override talk") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(columnWidth, 24f),
                            UpdateBounds = false,
                            LabelCentered = false
                        });

                        Dictionary<string, string>.KeyCollection speechLines = typeof(GameText).GetPrivateStatic<Dictionary<string, string>>("Fallback").Keys;
                        columns[1].Widgets.Add(new ButtonWidget(Game, "+ SPEECH", delegate() {
                            ContainerWidget widgetSpeech = null;
                            TextFieldWidget widgetFieldSpeech = null;
                            columns[1].Widgets.Insert(columns[1].Widgets.Count - 2, widgetSpeech = new ContainerWidget(Game, new GuiWidget[] {
                                widgetFieldSpeech = new TextFieldWidget(Game) {
                                    Size = new Vector2(columnWidth - 24f, 24f),
                                    UpdateBounds = false,
                                    Position = new Vector2(0f, 0f)
                                },
                                new ButtonWidget(Game, "X", delegate() {
                                    columns[1].Widgets.Remove(widgetSpeech);
                                }) {
                                    Background = new Color(0.5f, 0f, 0f, 1f),
                                    Size = new Vector2(24f, 24f),
                                    UpdateBounds = false,
                                    LabelCentered = true,
                                    Position = new Vector2(columnWidth - 24f, 0f)
                                }
                            }) {
                                Size = new Vector2(columnWidth, 24f),
                                Background = new Color(DefaultBackground, 0f)
                            });
                            widgetFieldSpeech.Fill(speechLines);
                        }) {
                            Background = new Color(0f, 0.125f, 0f, 1f),
                            Size = new Vector2(columnWidth, 24f),
                            UpdateBounds = false,
                            LabelCentered = true
                        });

                        columns[0].Widgets.Add(new ButtonWidget(Game, "ADD NPC", delegate() {
                            string[] positionSplit = windowFieldPosition.Text.Split(new char[] {';'});
                            string[] destinationSplit = windowFieldDestination.Text.Split(new char[] {';'});
                            NpcInstance npc = new NpcInstance() {
                                Id = int.Parse(windowFieldId.Text),
                                Name = windowFieldName.Text,
                                Position = new Vector3(
                                    float.Parse(positionSplit[0].Trim()),
                                    float.Parse(positionSplit[1].Trim()),
                                    float.Parse(positionSplit[2].Trim())
                                ),
                                DestinationOffset = new Vector3(
                                    float.Parse(destinationSplit[0].Trim()),
                                    float.Parse(destinationSplit[1].Trim()),
                                    float.Parse(destinationSplit[2].Trim())
                                ),
                                WalkSpeed = float.Parse(windowFieldWalkSpeed.Text),
                                AvoidsGomez = windowCheckboxAvoidsGomez.Checked
                            };

                            for (int ri = 0; ri < columns[1].Widgets.Count; ri++) {
                                GuiWidget rowWidget = columns[1].Widgets[ri];
                                if (!(rowWidget is ContainerWidget) || rowWidget.Widgets.Count != 2) {
                                    continue;
                                }
                                TextFieldWidget lineWidget = (TextFieldWidget) rowWidget.Widgets[0];
                                SpeechLine line = new SpeechLine() {
                                    Text = lineWidget.Text
                                };
                                if (ri == 0 && windowCheckboxOverrideTalk.Checked) {
                                    line.OverrideContent = new NpcActionContent() {
                                        AnimationName = "Talk"
                                    };
                                }
                                npc.Speech.Add(line);
                            }
                                

                            string root = ContentPaths.CharacterAnimations + "/" + npc.Name;
                            IEnumerable<string> list = CMProvider.GetAllIn(root);
                            foreach (string item_ in list) {
                                string item = item_.Substring(root.Length + 1).ToUpper();
                                NpcAction key;
                                if (!Enum.TryParse<NpcAction>(item, true, out key)) {
                                    continue;
                                }
                                npc.Actions[key] = new NpcActionContent() {
                                    AnimationName = key.ToString()
                                };
                            }

                            LevelManager.NonPlayerCharacters[npc.Id] = npc;
                            npc.State = new GameNpcState(Game, npc);
                            ServiceHelper.AddComponent(npc.State);
                            npc.State.Initialize();
                            ServiceHelper.Get<NpcHost>().GetPrivate<List<NpcState>>("NpcStates").Add(npc.State);

                            windowAddHeader.CloseButtonWidget.Action();
                            window.Refresh();
                        }) {
                            Size = new Vector2(columnWidth, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(0f, columns[0].Size.Y - 24f)
                        });

                        windowAdd.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (windowAdd.Size.X / 2);
                        windowAdd.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (windowAdd.Size.Y / 2);
                    }) {
                        Background = new Color(0f, 0.125f, 0f, 1f),
                        Size = new Vector2(window.Size.X, 24f),
                        UpdateBounds = false,
                        LabelCentered = true,
                        Position = new Vector2(0f, window.Size.Y - 24f)
                    });

                    return null;
                };

                window.Refresh();

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Paths", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true
                });
                window.Size.X = 128f;
                window.Size.Y = 24f;
                window.Label = "Paths";
                window.Widgets.Add(new WindowHeaderWidget(Game));

                int i = 0;
                foreach (MovementPath path in LevelManager.Paths.Values) {
                    window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                        new ButtonWidget(Game, path.Id.ToString()) {
                            Size = new Vector2(window.Size.X - 48f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        },
                        new ButtonWidget(Game, "C") {
                            Background = new Color(0f, 0f, 0.125f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 48f, 0f)
                        },
                        new ButtonWidget(Game, "X") {
                            Background = new Color(0.5f, 0f, 0f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 24f, 0f)
                        }
                    }) {
                        Size = new Vector2(window.Size.X, 24f),
                        Background = new Color(DefaultBackground, 0f)
                    });

                    i++;
                }

                window.Size.Y += i * 24f;
                window.Size.Y = Math.Min(512f, window.Size.Y);

                window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                }) {
                    Background = new Color(0f, 0.125f, 0f, 1f),
                    Size = new Vector2(window.Size.X, 24f),
                    UpdateBounds = false,
                    LabelCentered = true,
                    Position = new Vector2(0f, window.Size.Y - 24f)
                });

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Muted Loops", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true,
                    Size = new Vector2(512f, 24f),
                    Label = "Muted Loops"
                });

                window.RefreshValue = delegate() {
                    window.Widgets.Clear();
                    window.Widgets.Add(new WindowHeaderWidget(Game));

                    int i = 0;
                    foreach (string loop in LevelManager.MutedLoops) {
                        window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                            new ButtonWidget(Game, loop) {
                                Size = new Vector2(window.Size.X - 24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = false,
                                Position = new Vector2(0f, 0f)
                            },
                            new ButtonWidget(Game, "X", delegate() {
                                LevelManager.MutedLoops.Remove(loop);
                                SoundManager.UpdateSongActiveTracks();
                                window.Refresh();
                            }) {
                                Background = new Color(0.5f, 0f, 0f, 1f),
                                Size = new Vector2(24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = true,
                                Position = new Vector2(window.Size.X - 24f, 0f)
                            }
                        }) {
                            Size = new Vector2(window.Size.X, 24f),
                            Background = new Color(DefaultBackground, 0f)
                        });

                        i++;
                    }

                    window.Size.Y = (i+1) * 24f;
                    window.Size.Y = Math.Min(512f, window.Size.Y);

                    window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                        ContainerWidget windowAdd;
                        Widgets.Add(windowAdd = new ContainerWidget(Game) {
                            Size = new Vector2(256f, 48f),
                            Label = "Add Muted Loop"
                        });
                        WindowHeaderWidget windowAddHeader;
                        windowAdd.Widgets.Add(windowAddHeader = new WindowHeaderWidget(Game));

                        ButtonWidget windowLabelName;
                        windowAdd.Widgets.Add(windowLabelName = new ButtonWidget(Game, "Name:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        });
                        TextFieldWidget windowFieldName;
                        windowAdd.Widgets.Add(windowFieldName = new TextFieldWidget(Game) {
                            Size = new Vector2(windowAdd.Size.X - windowLabelName.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelName.Size.X, windowLabelName.Position.Y)
                        });
                        List<Loop> keysOrig = SoundManager.CurrentlyPlayingSong.Loops;
                        List<string> keys = new List<string>();
                        foreach (Loop keyOrig in keysOrig) {
                            keys.Add(keyOrig.Name);
                        }
                        windowFieldName.Fill(keys);

                        windowAdd.Widgets.Add(new ButtonWidget(Game, "ADD", delegate() {
                            LevelManager.MutedLoops.Add(windowFieldName.Text);
                            SoundManager.UpdateSongActiveTracks();
                            windowAddHeader.CloseButtonWidget.Action();
                            window.Refresh();
                        }) {
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(0f, windowAdd.Size.Y - 24f)
                        });

                        windowAdd.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (windowAdd.Size.X / 2);
                        windowAdd.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (windowAdd.Size.Y / 2);
                    }) {
                        Background = new Color(0f, 0.125f, 0f, 1f),
                        Size = new Vector2(window.Size.X, 24f),
                        UpdateBounds = false,
                        LabelCentered = true,
                        Position = new Vector2(0f, window.Size.Y - 24f)
                    });

                    return null;
                };
                window.Refresh();

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Ambience Tracks", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true,
                    Size = new Vector2(256f, 24f),
                    Label = "Ambience Tracks"
                });

                window.RefreshValue = delegate() {
                    window.Widgets.Clear();
                    window.Widgets.Add(new WindowHeaderWidget(Game));

                    int i = 0;
                    foreach (AmbienceTrack track in LevelManager.AmbienceTracks) {
                        window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                            new ButtonWidget(Game, track.Name) {
                                Size = new Vector2(window.Size.X - 24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = false,
                                Position = new Vector2(0f, 0f)
                            },
                            new ButtonWidget(Game, "X", delegate() {
                                LevelManager.AmbienceTracks.Remove(track);
                                SoundManager.PlayNewAmbience();
                                window.Refresh();
                            }) {
                                Background = new Color(0.5f, 0f, 0f, 1f),
                                Size = new Vector2(24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = true,
                                Position = new Vector2(window.Size.X - 24f, 0f)
                            }
                        }) {
                            Size = new Vector2(window.Size.X, 24f),
                            Background = new Color(DefaultBackground, 0f)
                        });

                        i++;
                    }

                    window.Size.Y = (i+1) * 24f;
                    window.Size.Y = Math.Min(512f, window.Size.Y);

                    window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                        ContainerWidget windowAdd;
                        Widgets.Add(windowAdd = new ContainerWidget(Game) {
                            Size = new Vector2(256f, 144f),
                            Label = "Add Ambience Track"
                        });
                        WindowHeaderWidget windowAddHeader;
                        windowAdd.Widgets.Add(windowAddHeader = new WindowHeaderWidget(Game));

                        ButtonWidget windowLabelName;
                        windowAdd.Widgets.Add(windowLabelName = new ButtonWidget(Game, "Name:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        });
                        TextFieldWidget windowFieldName;
                        windowAdd.Widgets.Add(windowFieldName = new TextFieldWidget(Game) {
                            Size = new Vector2(windowAdd.Size.X - windowLabelName.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelName.Size.X, windowLabelName.Position.Y)
                        });
                        Dictionary<string, string>.KeyCollection keysOrig = SoundManager.GetPrivate<Dictionary<string, string>>("MusicAliases").Keys;
                        List<string> keys = new List<string>();
                        foreach (string keyOrig in keysOrig) {
                            if (!keyOrig.StartsWith("ambience")) {
                                continue;
                            }
                            string key = keyOrig.Replace("\\", " ^ ").Replace("ambience", "Ambience");
                            keys.Add(key);
                        }
                        windowFieldName.Fill(keys);

                        CheckboxWidget windowCheckDawn;
                        windowAdd.Widgets.Add(windowCheckDawn = new CheckboxWidget(Game, "Dawn") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 24f)
                        });

                        CheckboxWidget windowCheckDay;
                        windowAdd.Widgets.Add(windowCheckDay = new CheckboxWidget(Game, "Day") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 48f)
                        });

                        CheckboxWidget windowCheckDusk;
                        windowAdd.Widgets.Add(windowCheckDusk = new CheckboxWidget(Game, "Dusk") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 72f)
                        });

                        CheckboxWidget windowCheckNight;
                        windowAdd.Widgets.Add(windowCheckNight = new CheckboxWidget(Game, "Night") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 96f)
                        });

                        windowAdd.Widgets.Add(new ButtonWidget(Game, "ADD", delegate() {
                            LevelManager.AmbienceTracks.Add(new AmbienceTrack() {
                                Name = windowFieldName.Text,
                                Dawn = windowCheckDawn.Checked,
                                Day = windowCheckDay.Checked,
                                Dusk = windowCheckDusk.Checked,
                                Night = windowCheckNight.Checked
                            });
                            SoundManager.PlayNewAmbience();
                            windowAddHeader.CloseButtonWidget.Action();
                            window.Refresh();
                        }) {
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(0f, windowAdd.Size.Y - 24f)
                        });

                        windowAdd.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (windowAdd.Size.X / 2);
                        windowAdd.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (windowAdd.Size.Y / 2);
                    }) {
                        Background = new Color(0f, 0.125f, 0f, 1f),
                        Size = new Vector2(window.Size.X, 24f),
                        UpdateBounds = false,
                        LabelCentered = true,
                        Position = new Vector2(0f, window.Size.Y - 24f)
                    });

                    return null;
                };
                window.Refresh();

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));

            TopBarWidget.Widgets.Add(button = new ButtonWidget(Game, "Settings"));
            button.Background.A = 0;
            button.Widgets.Add(new ButtonWidget(Game, "Invert theme", delegate() {
                FezEditor.Settings.DefaultBackground.R = (byte) (255 - FezEditor.Settings.DefaultBackground.R);
                FezEditor.Settings.DefaultBackground.G = (byte) (255 - FezEditor.Settings.DefaultBackground.G);
                FezEditor.Settings.DefaultBackground.B = (byte) (255 - FezEditor.Settings.DefaultBackground.B);

                FezEditor.Settings.DefaultForeground.R = (byte) (255 - FezEditor.Settings.DefaultForeground.R);
                FezEditor.Settings.DefaultForeground.G = (byte) (255 - FezEditor.Settings.DefaultForeground.G);
                FezEditor.Settings.DefaultForeground.B = (byte) (255 - FezEditor.Settings.DefaultForeground.B);

                foreach (GuiWidget widget in Widgets) {
                    widget.UpdateTheme();
                }
            }));
            CheckboxWidget checkboxShowAOTooltips;
            button.Widgets.Add(checkboxShowAOTooltips = new CheckboxWidget(Game, "ArtObject tooltips") {
                RefreshValue = () => FezEditor.Settings.TooltipArtObjectInfo
            });
            TextFieldWidget fieldBackupHistory;
            button.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                new ButtonWidget(Game, "Backup Depth:") {
                    Background = new Color(DefaultBackground, 0f),
                    LabelCentered = false,
                    Position = new Vector2(0f, 0f)
                },
                fieldBackupHistory = new TextFieldWidget(Game) {
                    RefreshValue = () => FezEditor.Settings.BackupHistory.ToString(),
                    Size = new Vector2(48f, 24f),
                    Position = new Vector2(144f, 0f)
                }
            }) {
                Size = new Vector2(192f, 24f)
            });
            button.Widgets.Add(new ButtonWidget(Game, "Save", delegate() {
                FezEditor.Settings.TooltipArtObjectInfo = checkboxShowAOTooltips.Checked;
                FezEditor.Settings.BackupHistory = int.Parse(fieldBackupHistory.Text);
                FezEditor.Settings.Save();
            }));

            //INFO
            Widgets.Add(InfoWidget = new EditorInfoWidget(Game));

            //ASSET PICKER
            AssetPickerWidgets.Add(AssetPickerWidget = new TrilePickerWidget(Game));
            AssetPickerLabels.Add(new ButtonWidget(Game, "Triles"));
            AssetPickerWidgets.Add(new ArtObjectPickerWidget(Game));
            AssetPickerLabels.Add(new ButtonWidget(Game, "Art Objects"));
            
            Widgets.Add(AssetPickerPickerWidget = new ContainerWidget(Game) {
                UpdateBounds = false,
                Size = new Vector2(0f, 24f)
            });
            AssetPickerPickerWidget.Widgets.AddRange(AssetPickerLabels);
            
            Widgets.AddRange(AssetPickerWidgets);

            //TOOLTIP
            TooltipWidget = new ButtonWidget(Game);

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

            TooltipWidget.Label = null;
            TooltipWidget.Position.X = MouseState.Position.X + 24f;
            TooltipWidget.Position.Y = MouseState.Position.Y;

            CursorHovering = false;

            Vector3 right = CameraManager.InverseView.Right;
            Vector3 up = CameraManager.InverseView.Up;
            Vector3 forward = CameraManager.InverseView.Forward;
            Ray ray = new Ray(GraphicsDevice.Viewport.Unproject(new Vector3(MouseState.Position.X, MouseState.Position.Y, 0.0f), CameraManager.Projection, CameraManager.View, Matrix.Identity), forward);
            float intersectionMin = float.MaxValue;

            HoveredTrile = null;
            //Ugly thread-safety workaround
            int trilesCount = LevelManager.Triles.Count;
            LevelManager.Triles.CopyTo(tmpTriles.Length < trilesCount ? (tmpTriles = new KeyValuePair<TrileEmplacement, TrileInstance>[trilesCount]) : tmpTriles, 0);
            for (int i = 0; i < trilesCount; i++) {
                TrileInstance trile = tmpTriles[i].Value;
                BoundingBox box = new BoundingBox(trile.Position, trile.Position + new Vector3(1f));
                float? intersection = ray.Intersects(box);
                if (intersection.HasValue && intersection < intersectionMin) {
                    HoveredTrile = trile;
                    HoveredBox = box;
                    intersectionMin = intersection.Value;
                }
            }

            HoveredAO = null;
            if (FezEditor.Settings.TooltipArtObjectInfo) {
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
            }

            if (HoveredAO != null) {
                TooltipWidget.Label = HoveredAO.Id + ": " + HoveredAO.ArtObjectName;
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

            if (cursorInMenu) {
                //CursorHovering = true;
                return;
            }

            if (HoveredTrile != null) {
                HoveredFace = GetHoveredFace(HoveredBox, ray);
                CursorHovering = true;
            }

            if (MouseState.LeftButton.State == MouseButtonStates.Clicked) {
                bool unfocusWidget = true;

                if (HoveredTrile != null) {
                    TrileEmplacement emplacement = new TrileEmplacement(HoveredTrile.Position - HoveredFace.AsVector());
                    if (FocusedWidget is TextFieldWidget) {
                        ((TextFieldWidget) FocusedWidget).Text = emplacement.X + "; " + emplacement.Y + "; " + emplacement.Z;
                        unfocusWidget = false;
                    } else if (LevelManager.TrileSet != null && LevelManager.TrileSet.Triles.ContainsKey(TrileId)) {
                        AddTrile(CreateNewTrile(TrileId, emplacement));
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
                TrileId = HoveredTrile.TrileId;
            }

            CameraManager.PixelsPerTrixel = Math.Max(0.25f, CameraManager.PixelsPerTrixel + 0.25f * MouseState.WheelTurns * WheelTurnsFactor);

            if (string.IsNullOrEmpty(TooltipWidget.Label)) {
                if (TooltipWidgetAdded) {
                    Widgets.Remove(TooltipWidget);
                    TooltipWidgetAdded = false;
                }
            } else {
                if (!TooltipWidgetAdded) {
                    Widgets.Add(TooltipWidget);
                    TooltipWidgetAdded = true;
                }
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

            foreach (GuiWidget widget in Widgets) {
                widget.GuiHandler = this;
                widget.Draw(gameTime);
            }

            SpriteBatch.Draw(cursor, 
                new Vector2(
                    (float) cursorPosition.X - cursorScale * CursorOffset.X,
                    (float) cursorPosition.Y - cursorScale * CursorOffset.Y
                ), null,
                new Color(1f, 1f, 1f, FezMath.Saturate((float) (1.0 - ((double) SinceMouseMoved - 2.0)))),
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
                if (update) {
                    widget.PreUpdate();
                    widget.Update(gameTime);
                }
                if (widget == TooltipWidget) {
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

            if (CameraManager.Viewpoint.IsOrthographic()) {
                trile.SetPhiLight(CameraManager.Viewpoint.ToPhi());
            } else {
                //TODO get closest viewpoint... somehow.
            }

            trile.PhysicsState = new InstancePhysicsState(trile) {
                Respawned = true,
                Vanished = false
            };
            trile.Enabled = true;

            return trile;
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

