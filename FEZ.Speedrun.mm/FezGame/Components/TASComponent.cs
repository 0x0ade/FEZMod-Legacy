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
using FezGame.Mod;
using FezGame.Speedrun;
using System.Text;
using FezGame.Speedrun.Clocks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using FezGame.Components.Actions;

namespace FezGame.Components {
    public class TASComponent : DrawableGameComponent {

        [ServiceDependency]
        public IGameService GameService { get; set; }
        [ServiceDependency]
        public IGameStateManager GameState { get; set; }
        [ServiceDependency]
        public IFontManager FontManager { get; set; }
        [ServiceDependency]
        public IGameLevelManager LevelManager { get; set; }
        [ServiceDependency]
        public ILevelMaterializer LevelMaterializer { get; set; }
        [ServiceDependency]
        public IKeyboardStateManager KeyboardState { get; set; }
        [ServiceDependency]
        public IInputManager InputManager { get; set; }
        [ServiceDependency]
        public IPlayerManager PlayerManager { get; set; }
        [ServiceDependency]
        public IGameCameraManager CameraManager { get; set; }

        public static TASComponent Instance;

        public SpriteBatch SpriteBatch { get; set; }
        public GlyphTextRenderer GTR { get; set; }

        public SpriteFont FontSmall;
        public float FontSmallFactor;
        public SpriteFont FontBig;
        public float FontBigFactor;

        private bool clockWasStrict;

        public List<Vector3> GomezPositions = new List<Vector3>();
        public List<Vector3> GomezVelocities = new List<Vector3>();
        public List<ActionType> GomezActions = new List<ActionType>();
        public List<Viewpoint> GomezRotations = new List<Viewpoint>();
        public List<Vector3> GomezCamPositions = new List<Vector3>();

        public TASComponent(Game game)
            : base(game) {
            UpdateOrder = 1000;
            DrawOrder = 3001;
            Instance = this;
        }

        public override void Initialize() {
            base.Initialize();

            SpriteBatch = new SpriteBatch(GraphicsDevice);
            GTR = new GlyphTextRenderer(Game);

            FontSmall = FontManager.Small;
            FontSmallFactor = 1f;
            FontBig = FontManager.Big;
            FontBigFactor = 1.5f;

            //KeyboardState.RegisterKey(Keys.D0);
        }

        public override void Update(GameTime gameTime) {
            if (FezSpeedrun.Clock == null || !FezSpeedrun.Clock.Running) {
                return;
            }

            if (InputManager.OpenInventory == FezButtonState.Down) {
                clockWasStrict = FezSpeedrun.Clock.Strict;
                FezSpeedrun.Clock.Strict = false;
                GameState.InMenuCube = true;
            } else {
                GameState.InMenuCube = false;
                FezSpeedrun.Clock.Strict = clockWasStrict;
            }

            if (GameState.InMenuCube && InputManager.CancelTalk == FezButtonState.Down && GomezPositions.Count > 0) {
                PlayerManager.Position = GomezPositions[GomezPositions.Count-1];
                GomezPositions.RemoveAt(GomezPositions.Count-1);

                PlayerManager.Velocity = GomezVelocities[GomezVelocities.Count-1];
                GomezVelocities.RemoveAt(GomezVelocities.Count-1);

                PlayerManager.Action = GomezActions[GomezActions.Count-1];
                PlayerManager.LastAction = PlayerManager.Action;
                PlayerManager.NextAction = PlayerManager.Action;
                GomezActions.RemoveAt(GomezActions.Count-1);

                CameraManager.ChangeViewpoint(GomezRotations[GomezRotations.Count-1]);
                GomezRotations.RemoveAt(GomezRotations.Count-1);

                CameraManager.InterpolatedCenter = GomezCamPositions[GomezCamPositions.Count-1];
                GomezCamPositions.RemoveAt(GomezCamPositions.Count-1);
            } else if (!GameState.InMenuCube) {
                GomezPositions.Add(PlayerManager.Position);

                GomezVelocities.Add(PlayerManager.Velocity);

                GomezActions.Add(PlayerManager.Action);

                GomezRotations.Add(CameraManager.Viewpoint);

                GomezCamPositions.Add(CameraManager.InterpolatedCenter);
            }
        }

        public override void Draw(GameTime gameTime) {
            if (!FEZMod.Preloaded || FezSpeedrun.Clock == null) {
                return;
            }

            Viewport viewport = GraphicsDevice.Viewport;
            float viewScale = GraphicsDevice.GetViewScale();

            GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
            SpriteBatch.BeginPoint();

            float lineBigHeight = FontBig.MeasureString("Time: 01:23:45.6789").Y * viewScale * FontBigFactor;

            if (GameState.InMenuCube) {
                GTR.DrawShadowedText(SpriteBatch, FontBig, "Time forcefully halted", new Vector2(0f, viewport.Height - lineBigHeight), Color.White, viewScale * FontBigFactor);
            }

            SpriteBatch.End();
        }

    }
}

