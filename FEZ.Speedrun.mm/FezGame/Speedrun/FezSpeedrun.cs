using System;
using System.IO;
using ContentSerialization;
using FezGame.Mod;
using FezEngine.Mod;
using FezEngine.Tools;
using FezGame.Components;
using FezEngine.Components;
using System.Collections.Generic;
using FezGame.Speedrun.Clocks;
using FezGame.Structure;
using FezGame.Tools;
using FezGame.Speedrun.BOT;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Speedrun {
    
    public class FezSpeedrunSettings : FezModuleSettings {
        public bool SpeedrunMode = false;
        public SpeedrunDisplayMode Display = SpeedrunDisplayMode.Current;
        public int LiveSplitPort = 16834;

        public bool ToolAssistedSpeedrun = false;
        public bool BOTEnabled = false;//Broken Optimizing Thing
    }
    
    public class FezSpeedrun : FezModule {

        public override string Name { get { return "FEZMod.Speedrun"; } }
        public override string Author { get { return "AngelDE98 & FEZMod contributors"; } }
        public override string Version { get { return FEZMod.Version; } }
        
        public static FezSpeedrunSettings Settings;

        public static ISpeedrunClock Clock;

        public static List<SplitCase> DefaultSplitCases = new List<SplitCase>();
        
        public static MenuLevel Menu;

        public FezSpeedrun() {
        }
        
        public override void Initialize() {
            Settings = FezModuleSettings.Load<FezSpeedrunSettings>("FEZMod.Speedrun.Settings.sdl", new FezSpeedrunSettings());
            
            if (Settings.SpeedrunMode) {
                FEZMod.EnableFEZometric = false;
                FEZMod.EnableQuickWarp = false;
                FEZMod.EnableBugfixes = false;
                FEZMod.EnableCustomIntros = false;
            }
            
            if (Settings.ToolAssistedSpeedrun) {
                FEZMod.EnableFEZometric = true;
                FEZMod.EnableQuickWarp = true;
                //Currently reqired unless someone hooks MovingGroupsHost and others to give a public instance
                FEZModEngine.GetComponentsAsServices = true;
                FEZModEngine.HandleComponents = true;
            }
            
            TextPatchHelper.Static.Fallback["FEZSpeedrunMenu"] = "SPEEDRUN MENU";
            TextPatchHelper.Static.Fallback["SpeedrunMode"] = "Mode: {0}";
            TextPatchHelper.Static.Fallback["DisplayMode"] = "Display time: {0}";
            TextPatchHelper.Static.Fallback["ToolAssist"] = "Tool assist (quicksaves, ...): {0}";
        }
        
        public override void LoadComponents(Fez game) {
            ServiceHelper.AddComponent(new SpeedrunInfo(ServiceHelper.Game));
            ServiceHelper.AddComponent(new TASComponent(ServiceHelper.Game));
        }

        public override void Exit() {
            if (Clock != null) {
                Clock.Running = false;
                Clock.Dispose();
            }
        }

        public override void HandleCrash(Exception e) {
            Exit();
        }
        
        public override void InitializeMenu(MenuBase mb) {
            FezSpeedrunSettings tmpSettings = null;
            Action save = delegate() {
                if (tmpSettings != null) {
                    Settings = tmpSettings;
                    Settings.Save();
                }
                tmpSettings = Settings.ShallowClone();
            };
            save();
            
            MenuItem item;
            
            float livesplitShownSince = 0f;
            const string livesplitText = "Visit https://livesplit.github.io/ for LiveSplit and the server component.";
            
            MenuItem displayMode = null;
            MenuItem toolAssist = null;
            
            Menu = new MenuLevel() {
                Title = "FEZSpeedrunMenu",
                AButtonString = "MenuApplyWithGlyph",
                BButtonString = "MenuSaveWithGlyph",
                IsDynamic = true,
                Oversized = true,
                Parent = FEZMod.Menu,
                OnReset = delegate() {
                },
                OnPostDraw = delegate(SpriteBatch batch, SpriteFont font, GlyphTextRenderer tr, float alpha) {
                    float scale = mb.Fonts.SmallFactor * batch.GraphicsDevice.GetViewScale();
                    float y = (float) batch.GraphicsDevice.Viewport.Height / 2f + (float) batch.GraphicsDevice.Viewport.Height / 3.825f;
                    if (tmpSettings.SpeedrunMode && tmpSettings.Display == SpeedrunDisplayMode.LiveSplit && mb.selectorPhase == SelectorPhase.Select) {
                        livesplitShownSince = Math.Min(livesplitShownSince + 0.05f, 1f);
                        tr.DrawCenteredString(batch, mb.Fonts.Small, livesplitText, new Color(1f, 1f, 1f, alpha * livesplitShownSince), new Vector2(0f, y), scale);
                    } else {
                        livesplitShownSince = Math.Max(livesplitShownSince - 0.1f, 0f);
                    }
                }
            };
            
            item = Menu.AddItem<string>("SpeedrunMode", save, false,
                () => (tmpSettings.SpeedrunMode) ? "SPEEDRUN" : "LET'S PLAY",
                delegate(string lastValue, int change) {
                    if (change != 0) {
                        tmpSettings.SpeedrunMode = !tmpSettings.SpeedrunMode;
                    }
                    
                    displayMode.Selectable = toolAssist.Selectable = tmpSettings.SpeedrunMode;
                    displayMode.Disabled = toolAssist.Disabled = !tmpSettings.SpeedrunMode;
                    
                    FEZMod.EnableFEZometric = !tmpSettings.SpeedrunMode;
                    FEZMod.EnableQuickWarp = !tmpSettings.SpeedrunMode;
                    FEZMod.EnableBugfixes = !tmpSettings.SpeedrunMode;
                    FEZMod.EnableCustomIntros = !tmpSettings.SpeedrunMode;
                    
                    ((MenuItem<string>) displayMode).SliderValueSetter(null, 0);
                    ((MenuItem<string>) toolAssist).SliderValueSetter(null, 0);
                }
            );
            item.UpperCase = true;
            
            displayMode = Menu.AddItem<string>("DisplayMode", save, false,
                delegate() {
                    switch (tmpSettings.Display) {
                        case SpeedrunDisplayMode.Current:
                            return "CURRENT ONLY";
                        case SpeedrunDisplayMode.CurrentPerRoom:
                            return "CURRENT + PER ROOM";
                        case SpeedrunDisplayMode.LiveSplit:
                            return "LIVESPLIT (" + tmpSettings.LiveSplitPort + ")";
                    }
                    return "UNKNOWN";
                },
                delegate(string lastValue, int change) {
                    int val = (int) tmpSettings.Display + change;
                    if (val < (int) SpeedrunDisplayMode.Current) {
                        val = (int) SpeedrunDisplayMode.Current;
                    }
                    if (val > (int) SpeedrunDisplayMode.LiveSplit) {
                        val = (int) SpeedrunDisplayMode.LiveSplit;
                    }
                    tmpSettings.Display = (SpeedrunDisplayMode) val;
                    
                }
            );
            displayMode.UpperCase = true;
            
            toolAssist = Menu.AddItem<string>("ToolAssist", save, false,
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
                }
            );
            toolAssist.UpperCase = true;
            
            displayMode.Selectable = toolAssist.Selectable = tmpSettings.SpeedrunMode;
            displayMode.Disabled = toolAssist.Disabled = !tmpSettings.SpeedrunMode;
            
            FEZMod.Menu.AddItem("FEZSpeedrunMenu", delegate() {
                mb.ChangeMenuLevel(Menu);
            });
            mb.MenuLevels.Add(Menu);
        }
        
        public static void StartClock() {
            //dispose old clock
            if (Clock != null && Clock.Running) {
                Clock.Running = false;
                Clock.Dispose();
                Clock = null;
            }

            //reset TAS progress if any
            TASComponent.Instance.RewindData.Clear();
            TASComponent.Instance.RewindPosition = 0;
            
            if (!Settings.SpeedrunMode) {
                return;
            }

            switch (Settings.Display) {
                case SpeedrunDisplayMode.Current:
                case SpeedrunDisplayMode.CurrentPerRoom:
                    Clock = new SpeedrunClock();
                    break;
                case SpeedrunDisplayMode.LiveSplit:
                    LiveSplitClock lsClock = new LiveSplitClock("localhost", Settings.LiveSplitPort);
                    lsClock.Clock = new SpeedrunClock();
                    Clock = lsClock;
                    break;
            }

            Clock.Running = true;
            //initialize BOT if needed
            if (Settings.BOTEnabled) {
                if (TASComponent.Instance.BOT == null) {
                    TASComponent.Instance.BOT = new FezGame.Speedrun.BOT.BOT(TASComponent.Instance);
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

