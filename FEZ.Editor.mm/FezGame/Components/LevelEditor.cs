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

namespace FezGame.Components {
    public class LevelEditor : DrawableGameComponent {

        [ServiceDependency]
        public IMouseStateManager MouseState { get; set; }
        [ServiceDependency]
        public ISoundManager SoundManager { get; set; }
        [ServiceDependency]
        public ITargetRenderingManager TargetRenderingManager { get; set; }
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

        private SpriteBatch SpriteBatch;
        private GlyphTextRenderer GTR;

        private float SinceMouseMoved = 3f;
        private Texture2D GrabbedCursor;
        private Texture2D CanClickCursor;
        private Texture2D ClickedCursor;
        private Texture2D PointerCursor;

        private DateTime BuildDate;

        public TrileInstance HoveredTrile;
        private KeyValuePair<TrileEmplacement, TrileInstance>[] tmpTriles = new KeyValuePair<TrileEmplacement, TrileInstance>[8192];

        /*
         * 111: Vase in Undefined
         * 732: Grey Small 01 in Random
         */
        public int TrileId = 111;//TODO let the player pick the ID manually.

        public LevelEditor(Game game)
            : base(game) {
            UpdateOrder = -10;
            DrawOrder = 2000;
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
        }

        public override void Update(GameTime gameTime) {
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
                float? intersection = ray.Intersects(new BoundingBox(trile.Position, trile.Position + new Vector3(1f)));
                if (intersection.HasValue && intersection < intersectionMin) {
                    HoveredTrile = trile;
                    intersectionMin = intersection.Value;
                }
            }

            if (MouseState.LeftButton.State == MouseButtonStates.Clicked && HoveredTrile != null) {
                TrileEmplacement emplacement = new TrileEmplacement(HoveredTrile.Position - ray.Direction);
                TrileInstance trile = new TrileInstance(emplacement, TrileId);
                trile.Position = HoveredTrile.Position - ray.Direction;
                LevelManager.Triles[emplacement] = trile;

                TrileGroup pickupGroup = new TrileGroup();
                pickupGroup.ActorType = trile.Trile.ActorSettings.Type;
                pickupGroup.Triles.Add(trile);
                LevelManager.PickupGroups[trile] = pickupGroup;
                for (int i = 0; i < 1024; i++) {
                    if (!LevelManager.Groups.ContainsKey(i)) {
                        LevelManager.Groups[i] = pickupGroup;
                        break;
                    }
                }

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
        }

        public override void Draw(GameTime gameTime) {
            Viewport viewport = GraphicsDevice.Viewport;

            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);

            float cursorScale = viewScale * 2f;
            Point cursorPosition = SettingsManager.PositionInViewport(MouseState);

            SpriteFont font = FontManager.Big;
            float fontScale = 1.5f * viewScale;

            string[] metadata = new string[] {
                "Build Date " + BuildDate,
                "Level: " + (LevelManager.Name ?? "(none)"),
                "Trile Set: " + (LevelManager.TrileSet != null ? LevelManager.TrileSet.Name : "(none)"),
                "Hovered Trile: " + (HoveredTrile != null ? (HoveredTrile.Trile.Name + " (" + HoveredTrile.Emplacement.X + ", " + HoveredTrile.Emplacement.Y + ", " + HoveredTrile.Emplacement.Z + ")") : "(none)"),
                "Hovered Trile ID: " + (HoveredTrile != null ? HoveredTrile.TrileId.ToString() : "(none)"),
                "Current View: " + CameraManager.Viewpoint,
                "Pixels per Trixel: " + CameraManager.PixelsPerTrixel
            };

            GraphicsDeviceExtensions.SetBlendingMode(GraphicsDevice, BlendingMode.Alphablending);
            GraphicsDeviceExtensions.BeginPoint(SpriteBatch);

            float lineHeight = font.MeasureString(metadata[0]).Y;
            for (int i = 0; i < metadata.Length; i++) {
                GTR.DrawShadowedText(SpriteBatch, font, metadata[i], new Vector2(0f, i * lineHeight), Color.White, fontScale);
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

        private DateTime ReadBuildDate() {
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

