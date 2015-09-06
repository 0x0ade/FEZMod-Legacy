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

        [ServiceDependency]
        public IGameLevelManager LevelManager { get; set; }
        [ServiceDependency]
        public ILevelMaterializer LevelMaterializer { get; set; }
        [ServiceDependency]
        public IKeyboardStateManager KeyboardState { get; set; }
        [ServiceDependency]
        public IPlayerManager PlayerManager { get; set; }
        [ServiceDependency]
        public IGameCameraManager CameraManager { get; set; }
        [ServiceDependency]
        public ITargetRenderingManager TRM { get; set; }

        public static TASComponent Instance;

        public ContainerWidget QuickSavesWidget;

        public BottomBarWidget BottomBarWidget;

        public bool Frozen = false;
        public TimeSpan MaxTime = new TimeSpan(0);

        public List<Vector3> GomezPositions = new List<Vector3>();
        public List<Vector3> GomezVelocities = new List<Vector3>();
        public List<ActionType> GomezActions = new List<ActionType>();
        public List<Viewpoint> GomezRotations = new List<Viewpoint>();
        public List<Vector3> GomezCamPositions = new List<Vector3>();

        public List<QuickSave> QuickSaves = new List<QuickSave>();

        protected QuickSave ThumbnailScheduled;
        protected RenderTargetHandle ThumbnailRT;

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

            //Quicksaves
            Widgets.Add(QuickSavesWidget = new ContainerWidget(Game) {
                Size = new Vector2(256f, 300f),
                UpdateBounds = true
            });
        }

        public override void Update(GameTime gameTime) {
            //Basic clock setup
            if (FezSpeedrun.Clock != null) {
                FezSpeedrun.Clock.InGame = false;
            }
            if (FezSpeedrun.Clock == null || !FezSpeedrun.Clock.Running) {
                base.Update(gameTime);
                BottomBarWidget.Position.Y = GraphicsDevice.Viewport.Height - BottomBarWidget.Size.Y;
                QuickSavesWidget.Position.X = GraphicsDevice.Viewport.Width - QuickSavesWidget.Size.X;
                QuickSavesWidget.Position.Y = BottomBarWidget.Position.Y - QuickSavesWidget.Size.Y;
                return;
            }

            //Freeze and rewind
            if (InputManager.OpenInventory == FezButtonState.Pressed) {
                Frozen = !Frozen;
                GameState.InMenuCube = Frozen;
            }
            if (Frozen) {
                FezSpeedrun.Clock.Strict = InputManager.CancelTalk == FezButtonState.Down;
            } else {
                FezSpeedrun.Clock.Strict = false;
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

            //Quicksave
            if (KeyboardState.GetKeyState(Keys.F6) == FezButtonState.Pressed) {
                //Save
                QuickSave();
            }

            if (KeyboardState.GetKeyState(Keys.F9) == FezButtonState.Pressed && QuickSaves.Count > 0) {
                //Load
                QuickLoad();
            }

            //Add quicksaves to the GUI
            if (QuickSavesWidget.Widgets.Count != QuickSaves.Count || (ThumbnailScheduled == null && ThumbnailRT != null)) {
                ThumbnailRT = null;
                QuickSavesWidget.Widgets.Clear();
                for (int i = 0; i < QuickSaves.Count; i++) {
                    QuickSavesWidget.Widgets.Insert(0, new QuickSaveWidget(Game, QuickSaves[i], QuickSavesWidget.Size.X));
                }
            }

            //GUI stuff
            base.Update(gameTime);

            BottomBarWidget.Position.Y = GraphicsDevice.Viewport.Height - BottomBarWidget.Size.Y;
            QuickSavesWidget.Position.X = GraphicsDevice.Viewport.Width - QuickSavesWidget.Size.X;
            QuickSavesWidget.Position.Y = BottomBarWidget.Position.Y - QuickSavesWidget.Size.Y;
        }

        public override void Draw(GameTime gameTime) {
            if (ThumbnailScheduled != null) {
                if (ThumbnailRT == null) {
                    base.Draw(gameTime);
                    ThumbnailRT = TRM.TakeTarget();
                    TRM.ScheduleHook(DrawOrder, ThumbnailRT.Target);
                    return;
                }

                TRM.Resolve(ThumbnailRT.Target, false);
                //TRM.ReturnTarget(ThumbnailRT);//?
                ThumbnailScheduled.Thumbnail = ThumbnailRT.Target;
                ThumbnailScheduled = null;
            }

            base.Draw(gameTime);
        }

        public void QuickSave() {
            QuickSave qs = new QuickSave();

            GameState.SaveData.CloneInto(qs.SaveData);
            ThumbnailScheduled = qs;

            qs.Time = FezSpeedrun.Clock.Time;
            qs.TimeLoading = FezSpeedrun.Clock.TimeLoading;

            qs.GomezPositions.AddRange(GomezPositions);
            qs.GomezVelocities.AddRange(GomezVelocities);
            qs.GomezActions.AddRange(GomezActions);
            qs.GomezRotations.AddRange(GomezRotations);
            qs.GomezCamPositions.AddRange(GomezCamPositions);
            QuickSaves.Add(qs);
        }

        public void QuickLoad(QuickSave qs = null) {
            if (qs == null) {
                qs = QuickSaves[QuickSaves.Count - 1];
            }

            qs.SaveData.CloneInto(GameState.SaveData);
            GameState.Loading = true;
            LevelManager.ChangeLevel(LevelManager.Name);
            GameState.ScheduleLoadEnd = true;
            PlayerManager.RespawnAtCheckpoint();
            LevelMaterializer.ForceCull();

            FezSpeedrun.Clock.Time = qs.Time;
            FezSpeedrun.Clock.TimeLoading = qs.TimeLoading;

            GomezPositions.Clear();
            GomezPositions.AddRange(qs.GomezPositions);
            GomezVelocities.Clear();
            GomezVelocities.AddRange(qs.GomezVelocities);
            GomezActions.Clear();
            GomezActions.AddRange(qs.GomezActions);
            GomezRotations.Clear();
            GomezRotations.AddRange(qs.GomezRotations);
            GomezCamPositions.Clear();
            GomezCamPositions.AddRange(qs.GomezCamPositions);

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
        }

    }
}

