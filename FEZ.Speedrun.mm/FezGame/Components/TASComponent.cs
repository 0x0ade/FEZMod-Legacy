using FezGame.Mod;
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
using FezGame.Mod.Gui;
using System.Drawing;

namespace FezGame.Components {
    public class TASComponent : AGuiHandler {

        public IGameLevelManager LevelManager { get; set; }
        [ServiceDependency]
        public ILevelMaterializer LevelMaterializer { get; set; }
        [ServiceDependency]
        public IKeyboardStateManager KeyboardState { get; set; }
        [ServiceDependency]
        public IPlayerManager PlayerManager { get; set; }
        [ServiceDependency]
        public IGameCameraManager CameraManager { get; set; }

        public static TASComponent Instance;

        public InfoWidget InfoWidget;

        public BottomBarWidget BottomBarWidget;

        public bool Frozen = false;
        public TimeSpan MaxTime = new TimeSpan(0);

        public List<Vector3> GomezPositions = new List<Vector3>();
        public List<Vector3> GomezVelocities = new List<Vector3>();
        public List<ActionType> GomezActions = new List<ActionType>();
        public List<Viewpoint> GomezRotations = new List<Viewpoint>();
        public List<Vector3> GomezCamPositions = new List<Vector3>();

        public TASComponent(Game game)
            : base(game) {
            UpdateOrder = 1000;
            DrawOrder = 4001;
            Instance = this;
        }

        public override void Initialize() {
            base.Initialize();

            //Register keys
            KeyboardState.RegisterKey(Keys.F6); //Quicksave
            KeyboardState.RegisterKey(Keys.F9); //Quickload

            //Add GUI

            //Bottom / progress bar
            Widgets.Add(BottomBarWidget = new BottomBarWidget(Game));

            //INFO
            Widgets.Add(InfoWidget = new InfoWidget(Game));
        }

        public override void Update(GameTime gameTime) {
            if (FezSpeedrun.Clock != null) {
                FezSpeedrun.Clock.InGame = false;
            }

            if (FezSpeedrun.Clock == null || !FezSpeedrun.Clock.Running) {
                base.Update(gameTime);
                BottomBarWidget.Position.Y = GraphicsDevice.Viewport.Height - BottomBarWidget.Size.Y;
                InfoWidget.Position.Y = BottomBarWidget.Position.Y - InfoWidget.Size.Y;
                return;
            }

            FezSpeedrun.Clock.Strict = false;

            if (InputManager.OpenInventory == FezButtonState.Pressed) {
                Frozen = !Frozen;
            }

            if (Frozen) {
                GameState.InMenuCube = InputManager.CancelTalk == FezButtonState.Up;
            } else {
                GameState.InMenuCube = false;
            }

            if (Frozen && InputManager.CancelTalk == FezButtonState.Down && GomezPositions.Count > 0) {
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

                //TODO make something automate this

                FezSpeedrun.Clock.Direction = -1D;
            } else if (!GameState.InMenuCube) {
                GomezPositions.Add(PlayerManager.Position);

                GomezVelocities.Add(PlayerManager.Velocity);

                GomezActions.Add(PlayerManager.Action);

                GomezRotations.Add(CameraManager.Viewpoint);

                GomezCamPositions.Add(CameraManager.InterpolatedCenter);
                //TODO make something automate this

                FezSpeedrun.Clock.Direction = 1D;

                if (FezSpeedrun.Clock.Time > MaxTime) {
                    MaxTime = FezSpeedrun.Clock.Time;
                }
            }

            base.Update(gameTime);

            BottomBarWidget.Position.Y = GraphicsDevice.Viewport.Height - BottomBarWidget.Size.Y;
            InfoWidget.Position.Y = BottomBarWidget.Position.Y - InfoWidget.Size.Y;
        }

    }
}

