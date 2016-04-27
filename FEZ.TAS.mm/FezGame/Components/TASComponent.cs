using FezGame.Mod;
using FezEngine.Mod;
using System;
using System.Collections.Generic;
using FezEngine.Services;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FezGame.Mod.Gui;
using System.Reflection;
using FezGame.TAS;
using FezGame.TAS.BOT;

namespace FezGame.Components {
    public class TASComponent : AGuiHandler {

        //TO-DO list created when watching MistahKurtz7
        //TODO add level chooser
        //TODO save cycle-based stuff

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

        public List<RewindInfo> RewindListening = new List<RewindInfo>();

        public ContainerWidget QuickSavesWidget;

        public BottomBarWidget BottomBarWidget;

        public bool Frozen = false;
        public TimeSpan MaxTime = new TimeSpan(0);

        public int RewindPosition = 0;
        public List<CacheKey_Info_Value[]> RewindData = new List<CacheKey_Info_Value[]>();

        public List<QuickSave> QuickSaves = new List<QuickSave>();
        
        public BOT BOT;

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
            
            //Register special key hooks
            Vector2 tmpFreeLook = new Vector2(0f, 0f);
            Func<Vector2> prev_get_FreeLook = FakeInputHelper.get_FreeLook;
            Action<Vector2> prev_set_FreeLook = FakeInputHelper.set_FreeLook;
            FakeInputHelper.get_FreeLook = delegate() {
                return !FezTAS.Settings.ToolAssistedSpeedrun || FakeInputHelper.Updating ? (prev_get_FreeLook != null ? prev_get_FreeLook() : tmpFreeLook) : new Vector2(0f, 0f);
            };
            FakeInputHelper.set_FreeLook = delegate(Vector2 value) {
                //Set game speed
                if (FezTAS.Settings.ToolAssistedSpeedrun && FakeInputHelper.Updating) {
                    FEZMod.GameSpeed = 1d + 0.5d * ((double) value.X) + 0.5d * ((double) value.Y);
                }
                tmpFreeLook = value;
                if (prev_set_FreeLook != null) {
                    prev_set_FreeLook(value);
                }
            };
            
            //Add GUI

            //Bottom / progress bar
            Widgets.Add(BottomBarWidget = new BottomBarWidget(Game));

            //Quicksaves
            QuickSavesWidget = new ContainerWidget(Game) {
                Size = new Vector2(256f, 300f),
                UpdateBounds = true
            };
            if (!FezTAS.Settings.BOTEnabled) {
                Widgets.Add(QuickSavesWidget);
            }

            /*
            RewindListening.Add(new RewindInfo(typeof(IDefaultCameraManager).GetFieldOrProperty("Viewpoint")) {
                Setter = (obj_, value_) => ((IDefaultCameraManager) obj_).ChangeViewpoint((Viewpoint) value_)
            });
            */

            RewindListening.Add(new MovingGroupsRewindInfo());

            FillRewindListening<IPlayerManager>();
            FillRewindListening<PlayerManager>(delegate(RewindInfo info) {
                info.InstanceGetter = () => ServiceHelper.Get<IPlayerManager>();
            });
            FillRewindListening<ITimeManager>();
            FillRewindListening<IDefaultCameraManager>();

            //DEBUG
            ModLogger.Log("FEZMod.TAS", "Listening to the following ");
            for (int i = 0; i < RewindListening.Count; i++) {
                RewindInfo info = RewindListening[i];
                if (info.Member != null) {
                    ModLogger.Log("FEZMod.TAS", i + ": " + info.Member.DeclaringType.FullName + "." + info.Member.Name);
                } else {
                    ModLogger.Log("FEZMod.TAS", i + ": black magic - " + info.GetType().Name);
                }
            }
        }

        public override void Update(GameTime gameTime) {
            if (!FezTAS.Settings.ToolAssistedSpeedrun) {
                return;
            }
            
            //Basic setup
            if (GameState.Loading) {
                base.Update(gameTime);
                BottomBarWidget.Position.Y = GraphicsDevice.Viewport.Height - BottomBarWidget.Size.Y;
                QuickSavesWidget.Position.X = GraphicsDevice.Viewport.Width - QuickSavesWidget.Size.X;
                QuickSavesWidget.Position.Y = BottomBarWidget.Position.Y - QuickSavesWidget.Size.Y;
                return;
            }
            
            //Schedule a BOT call
            if (BOT != null) {
                BOT.Update(gameTime);
            } else {
                //Freeze and rewind
                if (InputManager.OpenInventory == FezButtonState.Pressed) {
                    Frozen = !Frozen;
                    GameState.InMenuCube = Frozen;
                }
                if (Frozen && InputManager.CancelTalk == FezButtonState.Down && RewindData.Count > 0) {
                    RewindFrame();
    
                    DefaultCameraManager.NoInterpolation = true;
                } else if (!GameState.InMenuCube && !FezTAS.Settings.BOTEnabled) {
                    RecordFrame();
    
                    DefaultCameraManager.NoInterpolation = false;
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
            }

            //GUI stuff
            base.Update(gameTime);

            BottomBarWidget.Position.Y = GraphicsDevice.Viewport.Height - BottomBarWidget.Size.Y;
            QuickSavesWidget.Position.X = GraphicsDevice.Viewport.Width - QuickSavesWidget.Size.X;
            QuickSavesWidget.Position.Y = BottomBarWidget.Position.Y - QuickSavesWidget.Size.Y;
        }

        public override void Draw(GameTime gameTime) {
            if (!FezTAS.Settings.ToolAssistedSpeedrun) {
                return;
            }
            
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

            RewindData.Clear();
            RewindData.AddRange(qs.RewindData);

            RewindPosition = RewindData.Count;
            RewindFrame();
            GameState.ScheduleLoadEnd = true;
        }

        public void RewindFrame() {
            if (RewindPosition <= 0 || RewindPosition > RewindData.Count | FezTAS.Settings.BOTEnabled) {
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
            if (RewindPosition < 0 || RewindPosition > RewindData.Count || FezTAS.Settings.BOTEnabled) {
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

                if (info.Member != null) {
                    if (info.Instance == null) {
                        info.Instance = ServiceHelper.Get(info.Member.DeclaringType);
                    }
                    if (info.Instance == null) {
                        //other cases?
                    }
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
                if (RewindListening[i].Member != null && member.DeclaringType == RewindListening[i].Member.DeclaringType && member.Name == RewindListening[i].Member.Name) {
                    return;
                }
            }

            if (member is PropertyInfo && (((PropertyInfo) member).GetGetMethod() == null || ((PropertyInfo) member).GetSetMethod() == null)) {
                return;
            }

            RewindInfo info = new RewindInfo(member);
            if (onCreate != null) {
                onCreate(info);
            }
            RewindListening.Add(info);
        }

    }
}

