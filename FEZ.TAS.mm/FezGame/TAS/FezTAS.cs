using System;
using System.IO;
using ContentSerialization;
using FezGame.Mod;
using FezEngine.Mod;
using FezEngine.Tools;
using FezGame.Components;
using FezEngine.Components;
using System.Collections.Generic;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using FezGame;
using Common;
using System.Diagnostics;

namespace FezGame.TAS {
    
    public class FezTASSettings : FezModuleSettings {
        public bool ToolAssistedSpeedrun = false;
        public bool BOTEnabled = false;//Broken Optimizing Thing
    }
    
    public class FezTAS : FezModule {

        public override string Name { get { return "FEZMod.TAS"; } }
        public override string Author { get { return "0x0ade & FEZMod contributors"; } }
        public override string Version { get { return FEZMod.Version; } }
        
        public static FezTASSettings Settings;
        
        private static FieldInfo f_SpeedRun_Began;
        public static bool SpeedRunBegan {
            get {
                return (bool) ReflectionHelper.GetValue(f_SpeedRun_Began, null);
            }
            set {
                ReflectionHelper.SetValue(f_SpeedRun_Began, value, null);
            }
        }
        private static FieldInfo f_SpeedRun_Timer;
        public static Stopwatch SpeedRunTimer {
            get {
                return (Stopwatch) ReflectionHelper.GetValue(f_SpeedRun_Timer, null);
            }
            set {
                ReflectionHelper.SetValue(f_SpeedRun_Timer, value, null);
            }
        }

        public FezTAS() {
        }
        
        public override void Initialize() {
            Settings = FezModuleSettings.Load<FezTASSettings>("FEZMod.TAS.Settings.sdl", new FezTASSettings());
            
            if (Settings.ToolAssistedSpeedrun) {
                FEZMod.EnableBugfixes = false;
                FEZMod.EnableCustomIntros = false;
                //Currently reqired unless someone hooks MovingGroupsHost and others to give a public instance
                FEZModEngine.GetComponentsAsServices = true;
                FEZModEngine.HandleComponents = true;
            }
            
            Type t_SpeedRun = typeof(SpeedRun);
            f_SpeedRun_Began = t_SpeedRun.GetField("Began", BindingFlags.Static | BindingFlags.NonPublic);
            f_SpeedRun_Timer = t_SpeedRun.GetField("Timer", BindingFlags.Static | BindingFlags.NonPublic);
            
            TextPatchHelper.Static.Fallback["ToolAssist"] = "Tool assist (quicksaves, ...): {0}";
        }
        
        public override void LoadComponents(Fez game) {
            ServiceHelper.AddComponent(new TASComponent(ServiceHelper.Game));
        }

        public override void InitializeMenu(MenuBase mb) {
            FezTASSettings tmpSettings = null;
            Action save = delegate() {
                if (tmpSettings != null) {
                    Settings = tmpSettings;
                    Settings.Save();
                }
                tmpSettings = Settings.ShallowClone();
            };
            save();
            
            MenuItem toolAssist = FEZMod.Menu.AddItem<string>("ToolAssist", save, false,
                () => (tmpSettings.ToolAssistedSpeedrun) ? (tmpSettings.BOTEnabled ? "BOT" : "ON") : "OFF",
                delegate(string lastValue, int change) {
                    if (change != 0) {
                        if ((!tmpSettings.ToolAssistedSpeedrun && !tmpSettings.BOTEnabled && change < 0) || (tmpSettings.ToolAssistedSpeedrun && tmpSettings.BOTEnabled && 0 < change)) {
                            //no-op
                        } else if (!tmpSettings.ToolAssistedSpeedrun && !tmpSettings.BOTEnabled && 0 < change) {
                            //OFF > ON
                            tmpSettings.ToolAssistedSpeedrun = true;
                            tmpSettings.BOTEnabled = false;
                        } else if (tmpSettings.ToolAssistedSpeedrun && !tmpSettings.BOTEnabled && 0 < change) {
                            //ON > BOT
                            tmpSettings.ToolAssistedSpeedrun = true;
                            tmpSettings.BOTEnabled = true;
                        } else if (tmpSettings.ToolAssistedSpeedrun && tmpSettings.BOTEnabled && change < 0) {
                            //BOT > ON
                            tmpSettings.ToolAssistedSpeedrun = true;
                            tmpSettings.BOTEnabled = false;
                        } else if (tmpSettings.ToolAssistedSpeedrun && !tmpSettings.BOTEnabled && change < 0) {
                            //ON > OFF
                            tmpSettings.ToolAssistedSpeedrun = false;
                            tmpSettings.BOTEnabled = false;
                        }
                    }
                    
                    FEZMod.EnableFEZometric = tmpSettings.ToolAssistedSpeedrun;
                    FEZMod.EnableQuickWarp = tmpSettings.ToolAssistedSpeedrun;
                    //Currently reqired unless someone hooks MovingGroupsHost and others to give a public instance
                    FEZModEngine.GetComponentsAsServices = tmpSettings.ToolAssistedSpeedrun;
                    FEZModEngine.HandleComponents = tmpSettings.ToolAssistedSpeedrun;
                    save();
                }
            );
            toolAssist.UpperCase = true;
        }
        
        public static void Start() {
            //reset TAS progress if any
            TASComponent.Instance.RewindData.Clear();
            TASComponent.Instance.RewindPosition = 0;
            
            if (!Settings.ToolAssistedSpeedrun) {
                return;
            }

            //initialize BOT if needed
            if (Settings.BOTEnabled) {
                if (TASComponent.Instance.BOT == null) {
                    TASComponent.Instance.BOT = new FezGame.TAS.BOT.BOT(TASComponent.Instance);
                } else {
                    TASComponent.Instance.BOT.Dispose();
                }
                TASComponent.Instance.BOT.Initialize();
            } else if (TASComponent.Instance.BOT != null) {
                TASComponent.Instance.BOT.Dispose();
                TASComponent.Instance.BOT = null;
            }
        }
        
    }
    
}

