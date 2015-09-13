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
using System.Reflection;

namespace FezGame.Components {
    public class TASComponent : AGuiHandler {

        //TO-DO list created when watching MistahKurtz7
        //TODO start timer when continuing old save
        //TODO add level chooser
        //TODO save cycle-based stuff
        //TODO fix font sizes

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

        public List<RewindInfo> RewindListening = new List<RewindInfo>() {
            new RewindInfo(typeof(IDefaultCameraManager).GetFieldOrProperty("Viewpoint")) {
                Setter = (obj_, value_) => ((IDefaultCameraManager) obj_).ChangeViewpoint((Viewpoint) value_)
            },
        };

        public ContainerWidget QuickSavesWidget;

        public BottomBarWidget BottomBarWidget;

        public bool Frozen = false;
        public TimeSpan MaxTime = new TimeSpan(0);

        public int RewindPosition = 0;
        public List<CacheKey_Info_Value[]> RewindData = new List<CacheKey_Info_Value[]>();

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
            
            FillRewindListening<IPlayerManager>();
            FillRewindListening<PlayerManager>(delegate(RewindInfo info) {
                info.InstanceGetter = () => ServiceHelper.Get<IPlayerManager>();
            });
            FillRewindListening<IDefaultCameraManager>();
        }

        public override void Update(GameTime gameTime) {
            //Basic clock setup
            if (FezSpeedrun.Clock != null) {
                FezSpeedrun.Clock.InGame = false;
            }
            if (FezSpeedrun.Clock == null || !FezSpeedrun.Clock.Running || GameState.Loading) {
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
            if (Frozen && InputManager.CancelTalk == FezButtonState.Down && RewindData.Count > 0) {
                RewindFrame();

                DefaultCameraManager.NoInterpolation = true;
                FezSpeedrun.Clock.Direction = -1D;
            } else if (!GameState.InMenuCube) {
                RecordFrame();

                DefaultCameraManager.NoInterpolation = false;
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

            qs.RewindData.AddRange(RewindData);
            QuickSaves.Add(qs);
        }

        public void QuickLoad(QuickSave qs = null) {
            if (qs == null) {
                qs = QuickSaves[QuickSaves.Count - 1];
            }

            GameState.Loading = true;
            qs.SaveData.CloneInto(GameState.SaveData);
            LevelManager.ChangeLevel(GameState.SaveData.Level);
            PlayerManager.RespawnAtCheckpoint();
            LevelMaterializer.ForceCull();

            FezSpeedrun.Clock.Time = qs.Time;
            FezSpeedrun.Clock.TimeLoading = qs.TimeLoading;

            RewindData.Clear();
            RewindData.AddRange(qs.RewindData);

            RewindPosition = RewindData.Count - 1;
            RewindFrame();
            GameState.ScheduleLoadEnd = true;
        }

        public void RewindFrame() {
            if (RewindPosition <= 0 || RewindPosition > RewindData.Count) {
                return;
            }

            RewindPosition--;
            CacheKey_Info_Value[] frame = RewindData[RewindPosition];

            for (int i = 0; i < frame.Length; i++) {
                CacheKey_Info_Value data = frame[i];
                RewindInfo info = data.Key;

                info.Set(data.Value);
            }
        }

        public void RecordFrame() {
            if (RewindPosition < 0 || RewindPosition > RewindData.Count) {
                return;
            }

            CacheKey_Info_Value[] frame;
            if (RewindPosition == RewindData.Count) {
                frame = new CacheKey_Info_Value[RewindListening.Count];
                RewindData.Add(frame);
            } else {
                frame = RewindData[RewindPosition];
            }
            RewindPosition++;

            for (int i = 0; i < RewindListening.Count; i++) {
                CacheKey_Info_Value data = new CacheKey_Info_Value();
                RewindInfo info = RewindListening[i];

                if (info.Instance == null) {
                    info.Instance = ServiceHelper.Get(info.Member.DeclaringType);
                }
                if (info.Instance == null) {
                    //other cases?
                }

                data.Key = info;
                data.Value = info.Get();
                frame[i] = data;
            }
        }
        
        public void FillRewindListening<T>(Action<RewindInfo> onCreate = null) {
            FillRewindListening(typeof(T), onCreate);
        }
        
        public void FillRewindListening(Type type, Action<RewindInfo> onCreate = null) {
            FieldInfo[] fields = type.GetFields();
            for (int i = 0; i < fields.Length; i++) {
              AddRewindListening(fields[i], onCreate);
            }
            PropertyInfo[] properties = type.GetProperties();
            for (int i = 0; i < properties.Length; i++) {
              AddRewindListening(properties[i], onCreate);
            }
        }
        
        public void AddRewindListening(MemberInfo member, Action<RewindInfo> onCreate = null) {
            for (int i = 0; i < RewindListening.Count; i++) {
                if (member.Equals(RewindListening[i].Member)) {
                    return;
                }
            }

            RewindInfo info = new RewindInfo(member);
            if (onCreate != null) {
                onCreate(info);
            }
            RewindListening.Add(info);
        }

    }
}

