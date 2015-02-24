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
        public IMouseStateManager MouseState { protected get; set; }
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
        public IDotManager DotManager { get; set; }
        [ServiceDependency]
        public ISpeechBubbleManager SpeechBubble { get; set; }
        [ServiceDependency]
        public IContentManagerProvider CMProvider { private get; set; }

        public static LevelEditor Instance;

        private SpriteBatch SpriteBatch;
        private GlyphTextRenderer GTR;

        private float SinceMouseMoved = 3f;
        private bool CursorSelectable = false;
        private Texture2D GrabbedCursor;
        private Texture2D CanClickCursor;
        private Texture2D ClickedCursor;
        private Texture2D PointerCursor;

        private DateTime BuildDate;

        public TrileInstance HoveredTrile;

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
            GTR = new GlyphTextRenderer(this.Game);

            PointerCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_POINTER");
            CanClickCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_CLICKER_A");
            ClickedCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_CLICKER_B");
            GrabbedCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_GRABBER");
        }

        public override void Update(GameTime gameTime) {
            SinceMouseMoved += (float) gameTime.ElapsedGameTime.TotalSeconds;
            if (MouseState.Movement.X != 0 || MouseState.Movement.Y != 0) {
                SinceMouseMoved = 0f;
            }

            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);

            /*Viewport viewport = ServiceHelper.Game.GraphicsDevice.Viewport;
            Point cursorPosition = SettingsManager.PositionInViewport(MouseState);
            Vector2 cursorWorldPosition = new Vector2(
                ((viewport.X + cursorPosition.X) / viewport.Width) * (16 * viewScale),
                ((viewport.Y + cursorPosition.Y) / viewport.Height) * (16 * viewScale)
            );*/

            //LevelManager.Triles;
        }

        public override void Draw(GameTime gameTime) {
            Viewport viewport = GraphicsDevice.Viewport;

            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);

            float cursorScale = viewScale * 2f;
            Point cursorPosition = SettingsManager.PositionInViewport(MouseState);

            SpriteFont font = FontManager.Big;
            float fontScale = 1.5f * viewScale;

            Matrix worldMatrix = new Matrix();

            Vector3 cursorNear = viewport.Unproject(new Vector3(cursorPosition.X, cursorPosition.Y, 0f), CameraManager.Projection, CameraManager.View, worldMatrix);
            Vector3 cursorFar = viewport.Unproject(new Vector3(cursorPosition.X, cursorPosition.Y, 1f), CameraManager.Projection, CameraManager.View, worldMatrix);
            Vector3 cursorDir = Vector3.Normalize(cursorFar - cursorNear);
            Ray cursorRay = new Ray(cursorNear, cursorDir);

            string[] metadata = new string[] {
                "Build Date " + BuildDate,
                "Level: " + (LevelManager.Name ?? "(none)"),
                "Trile Set: " + (LevelManager.TrileSet != null ? LevelManager.TrileSet.Name : "(none)"),
                "Hovered Trile: " + (HoveredTrile != null ? (HoveredTrile.VisualTrile.Name + " (" + HoveredTrile.Emplacement.X + ", " + HoveredTrile.Emplacement.Y + ", " + HoveredTrile.Emplacement.Z + ")") : "(none)"),
                "Current View: " + CameraManager.Viewpoint,
                "Gomez Position: (" + PlayerManager.Position.X + ", " + PlayerManager.Position.Y + ", " + PlayerManager.Position.Z + ")",
                "Pixels per Trixel: " + CameraManager.PixelsPerTrixel
            };

            GraphicsDeviceExtensions.SetBlendingMode(GraphicsDevice, BlendingMode.Alphablending);
            GraphicsDeviceExtensions.BeginPoint(SpriteBatch);

            float lineHeight = font.MeasureString(metadata[0]).Y;
            for (int i = 0; i < metadata.Length; i++) {
                GTR.DrawShadowedText(SpriteBatch, font, metadata[i], new Vector2(0f, i * lineHeight), Color.White, fontScale);
            }

            SpriteBatch.Draw(MouseState.LeftButton.State == MouseButtonStates.Dragging || MouseState.RightButton.State == MouseButtonStates.Dragging ? GrabbedCursor : (CursorSelectable ? (MouseState.LeftButton.State == MouseButtonStates.Down ? ClickedCursor : CanClickCursor) : PointerCursor), 
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

