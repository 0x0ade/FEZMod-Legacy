using Common;
using System;
using System.Collections.Generic;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using FezGame.Editor.Widgets;

namespace FezGame.Components {
    public class LevelEditor : DrawableGameComponent, ILevelEditor {

        [ServiceDependency]
        public IMouseStateManager MouseState { get; set; }
        [ServiceDependency]
        public ISoundManager SoundManager { get; set; }
        [ServiceDependency]
        public IInputManager InputManager { get; set; }
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

        public static LevelEditor Instance;

        public SpriteBatch SpriteBatch { get; set; }
        public GlyphTextRenderer GTR { get; set; }

        protected float SinceMouseMoved = 3f;
        protected Texture2D GrabbedCursor;
        protected Texture2D CanClickCursor;
        protected Texture2D ClickedCursor;
        protected Texture2D PointerCursor;

        public DateTime BuildDate { get; protected set; }

        protected KeyValuePair<TrileEmplacement, TrileInstance>[] tmpTriles = new KeyValuePair<TrileEmplacement, TrileInstance>[8192];

        public TrileInstance HoveredTrile { get; set; }
        public BoundingBox HoveredBox { get; set; }
        public FaceOrientation HoveredFace { get; set; }
        public int TrileId { get; set; }//TODO let the player pick the ID in a better way than scrolling.

        public List<EditorWidget> Widgets = new List<EditorWidget>();

        public InfoWidget InfoWidget;
        public TopBarWidget TopBarWidget;

        public LevelEditor(Game game)
            : base(game) {
            UpdateOrder = -10;
            DrawOrder = 3000;
            TrileId = 0;
            Instance = this;
        }

        public override void Initialize() {
            base.Initialize();

            BuildDate = ReadBuildDate();

            SpriteBatch = new SpriteBatch(GraphicsDevice);
            GTR = new GlyphTextRenderer(Game);

            PointerCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_POINTER");
            CanClickCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_CLICKER_A");
            ClickedCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_CLICKER_B");
            GrabbedCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_GRABBER");

            //GameState.InEditor = true;//Causes some graphical funkyness.

            Widgets.Add(TopBarWidget = new TopBarWidget(Game));
            Widgets.Add(InfoWidget = new InfoWidget(Game));
        }

        public override void Update(GameTime gameTime) {
            if (GameState.InMap || GameState.Loading) {
                return;
            }

            SinceMouseMoved += (float) gameTime.ElapsedGameTime.TotalSeconds;
            if (MouseState.Movement.X != 0 || MouseState.Movement.Y != 0) {
                SinceMouseMoved = 0f;
            }

            HoveredTrile = null;

            Vector3 right = CameraManager.InverseView.Right;
            Vector3 up = CameraManager.InverseView.Up;
            Vector3 forward = CameraManager.InverseView.Forward;
            Ray ray = new Ray(GraphicsDevice.Viewport.Unproject(new Vector3(MouseState.Position.X, MouseState.Position.Y, 0.0f), CameraManager.Projection, CameraManager.View, Matrix.Identity), forward);
            float intersectionMin = float.MaxValue;

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

            if (HoveredTrile != null) {
                HoveredFace = GetHoveredFace(HoveredBox, ray);
            }

            if (MouseState.LeftButton.State == MouseButtonStates.Clicked && HoveredTrile != null && LevelManager.TrileSet != null && LevelManager.TrileSet.Triles.ContainsKey(TrileId)) {
                TrileEmplacement emplacement = new TrileEmplacement(HoveredTrile.Position - FezMath.AsVector(HoveredFace));
                TrileInstance trile = new TrileInstance(emplacement, TrileId);
                LevelManager.Triles[emplacement] = trile;

                trile.SetPhiLight(CameraManager.Viewpoint.ToPhi());

                trile.PhysicsState = new InstancePhysicsState(trile);
                trile.Enabled = true;

                trile.Update();
                LevelMaterializer.AddInstance(trile);
                LevelMaterializer.RebuildTriles(true);
                LevelMaterializer.RebuildInstances();
                LevelMaterializer.UpdateInstance(trile);
                trile.RefreshTrile();
            }

            if (MouseState.RightButton.State == MouseButtonStates.Clicked && HoveredTrile != null) {
                LevelManager.ClearTrile(HoveredTrile);
                HoveredTrile = null;
            }

            if (MouseState.MiddleButton.State == MouseButtonStates.Clicked && HoveredTrile != null) {
                TrileId = HoveredTrile.TrileId;
            }

            if (MouseState.WheelTurnedUp == FezButtonState.Pressed) {
                TrileId++;
            }

            if (MouseState.WheelTurnedDown == FezButtonState.Pressed) {
                TrileId--;
            }
        }

        public override void Draw(GameTime gameTime) {
            if (GameState.InMap || GameState.Loading) {
                return;
            }

            Viewport viewport = GraphicsDevice.Viewport;
            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);

            float cursorScale = viewScale * 2f;
            Point cursorPosition = SettingsManager.PositionInViewport(MouseState);


            GraphicsDeviceExtensions.SetBlendingMode(GraphicsDevice, BlendingMode.Alphablending);
            GraphicsDeviceExtensions.BeginPoint(SpriteBatch);

            InfoWidget.Position.Y = TopBarWidget.Position.Y + TopBarWidget.Size.Y;

            foreach (EditorWidget widget in Widgets) {
                widget.LevelEditor = this;
                widget.Draw(gameTime);
            }

            SpriteBatch.Draw(MouseState.LeftButton.State == MouseButtonStates.Dragging || MouseState.RightButton.State == MouseButtonStates.Dragging ? GrabbedCursor : (HoveredTrile != null ? (MouseState.LeftButton.State == MouseButtonStates.Down || MouseState.RightButton.State == MouseButtonStates.Down ? ClickedCursor : CanClickCursor) : PointerCursor), 
                new Vector2(
                    (float) cursorPosition.X - cursorScale * 11.5f,
                    (float) cursorPosition.Y - cursorScale * 8.5f
                ), new Rectangle?(),
                new Color(1f, 1f, 1f, FezMath.Saturate((float) (1.0 - ((double) SinceMouseMoved - 2.0)))),
                0.0f,
                Vector2.Zero,
                cursorScale,
                SpriteEffects.None,
                0.0f);

            SpriteBatch.End();
        }

        protected FaceOrientation GetHoveredFace(BoundingBox box, Ray ray) {
            float intersectionMin = float.MaxValue;

            BoundingBox[] sides = new BoundingBox[6];
            sides[0] = new BoundingBox(new Vector3(box.Min.X, box.Min.Y, box.Min.Z), new Vector3(box.Min.X, box.Max.Y, box.Max.Z));
            sides[1] = new BoundingBox(new Vector3(box.Max.X, box.Min.Y, box.Min.Z), new Vector3(box.Max.X, box.Max.Y, box.Max.Z));
            sides[2] = new BoundingBox(new Vector3(box.Min.X, box.Min.Y, box.Min.Z), new Vector3(box.Max.X, box.Min.Y, box.Max.Z));
            sides[3] = new BoundingBox(new Vector3(box.Min.X, box.Max.Y, box.Min.Z), new Vector3(box.Max.X, box.Max.Y, box.Max.Z));
            sides[4] = new BoundingBox(new Vector3(box.Min.X, box.Min.Y, box.Min.Z), new Vector3(box.Max.X, box.Max.Y, box.Min.Z));
            sides[5] = new BoundingBox(new Vector3(box.Min.X, box.Min.Y, box.Max.Z), new Vector3(box.Max.X, box.Max.Y, box.Max.Z));

            FaceOrientation[] faces = new FaceOrientation[6];
            faces[0] = FaceOrientation.Right;
            faces[1] = FaceOrientation.Left;
            faces[2] = FaceOrientation.Top;
            faces[3] = FaceOrientation.Down;
            faces[4] = FaceOrientation.Front;
            faces[5] = FaceOrientation.Back;

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

        protected DateTime ReadBuildDate() {
            string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
            byte[] b = new byte[2048];
            Stream s = null;

            try {
                s = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                s.Read(b, 0, 2048);
            } finally {
                if (s != null) {
                    s.Close();
                }
            }

            int i = BitConverter.ToInt32(b, c_PeHeaderOffset);
            int secondsSince1970 = BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.ToLocalTime();
            return dt;
        }
    }
}

