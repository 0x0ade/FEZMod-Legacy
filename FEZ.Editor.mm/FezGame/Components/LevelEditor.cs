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

        private float SinceMouseMoved = 3f;
        private bool CursorSelectable = false;
        private Texture2D GrabbedCursor;
        private Texture2D CanClickCursor;
        private Texture2D ClickedCursor;
        private Texture2D PointerCursor;

        public LevelEditor(Game game)
            : base(game) {
            UpdateOrder = -10;
            DrawOrder = 1000;
            Instance = this;
        }

        public override void Initialize() {
            base.Initialize();
            SpriteBatch = new SpriteBatch(this.GraphicsDevice);
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
        }

        public override void Draw(GameTime gameTime) {
            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);
            float scale2 = viewScale * 2f;
            Point point = SettingsManager.PositionInViewport(MouseState);
            SpriteBatch.Begin();
            SpriteBatch.Draw(MouseState.LeftButton.State == MouseButtonStates.Dragging || MouseState.RightButton.State == MouseButtonStates.Dragging ? this.GrabbedCursor : (CursorSelectable ? (this.MouseState.LeftButton.State == MouseButtonStates.Down ? this.ClickedCursor : this.CanClickCursor) : this.PointerCursor), new Vector2((float) point.X - scale2 * 11.5f, (float) point.Y - scale2 * 8.5f), new Rectangle?(), new Color(1f, 1f, 1f, FezMath.Saturate((float) (1.0 - ((double) this.SinceMouseMoved - 2.0)))), 0.0f, Vector2.Zero, scale2, SpriteEffects.None, 0.0f);
            SpriteBatch.End();
        }
    }
}

