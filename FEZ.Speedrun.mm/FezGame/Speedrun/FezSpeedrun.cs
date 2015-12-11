using System;
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
    public class FezSpeedrun : FezModule {

        public override string Name { get { return "FEZMod.Speedrun"; } }
        public override string Author { get { return "AngelDE98 & FEZMod contributors"; } }
        public override string Version { get { return FEZMod.Version; } }

        public static bool SpeedrunMode = false;
        public static SpeedrunDisplayMode Display = SpeedrunDisplayMode.Current;
        public static int LiveSplitPort = 16834;

        public static bool ToolAssistedSpeedrun = false;
        public static bool BOTEnabled = false;//Broken Optimizing Thing

        public static ISpeedrunClock Clock;

        public static List<SplitCase> DefaultSplitCases = new List<SplitCase>();
        
        public static MenuLevel Menu;

        public FezSpeedrun() {
        }
        
        public override void Initialize() {
            //TODO load config
            if (SpeedrunMode) {
                FEZMod.EnableFEZometric = false;
                FEZMod.EnableQuickWarp = false;
                FEZMod.EnableBugfixes = false;
            }
            
            if (ToolAssistedSpeedrun) {
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

        public override void ParseArgs(string[] args) {
            for (int i = 0; i < args.Length; i++) {
                if (Clock != null && (args[i] == "-tas" || args[i] == "--tool-assisted-speedrun")) {
                    ModLogger.Log("FEZMod", "Found -tas / --tool-assisted-speedrun");
                    ModLogger.Log("FEZMod", "C'MON! RAHRHH");
                }
                if (Clock != null && (args[i] == "-bot" || args[i] == "--bot")) {
                    ModLogger.Log("FEZMod", "Found -bot / --bot");
                    BOTEnabled = true;
                }
            }
        }
        
        public override void LoadComponents(Fez game) {
            Action load = delegate() {
                ServiceHelper.AddComponent(new SpeedrunInfo(ServiceHelper.Game));
                ServiceHelper.AddComponent(new TASComponent(ServiceHelper.Game));
            };
            #if FNA
            //Proper way would be to only initialize what's required via FezEngine.Tools.DrawActionScheduler.Schedule, but.. yeah.
            //SI and TASC aren't required pre draw, so they can get added later.
            DrawActionScheduler.Schedule(load);
            #else
            load();
            #endif
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
                    if (SpeedrunMode && Display == SpeedrunDisplayMode.LiveSplit && mb.selectorPhase == SelectorPhase.Select) {
                        livesplitShownSince = Math.Min(livesplitShownSince + 0.05f, 1f);
                        tr.DrawCenteredString(batch, mb.Fonts.Small, livesplitText, new Color(1f, 1f, 1f, alpha * livesplitShownSince), new Vector2(0f, y), scale);
                    } else {
                        livesplitShownSince = Math.Max(livesplitShownSince - 0.1f, 0f);
                    }
                }
            };
            
            item = Menu.AddItem<string>("SpeedrunMode", delegate() {
                    //onSelect
                }, false,
                () => (SpeedrunMode) ? "SPEEDRUN" : "LET'S PLAY",
                delegate(string lastValue, int change) {
                    if (change != 0) {
                        SpeedrunMode = !SpeedrunMode;
                    }
                    
                    displayMode.Selectable = toolAssist.Selectable = SpeedrunMode;
                    displayMode.Disabled = toolAssist.Disabled = !SpeedrunMode;
                    
                    FEZMod.EnableFEZometric = !SpeedrunMode;
                    FEZMod.EnableQuickWarp = !SpeedrunMode;
                    FEZMod.EnableBugfixes = !SpeedrunMode;
                    
                    ((MenuItem<string>) displayMode).SliderValueSetter(null, 0);
                    ((MenuItem<string>) toolAssist).SliderValueSetter(null, 0);
                }
            );
            item.UpperCase = true;
            
            displayMode = Menu.AddItem<string>("DisplayMode", delegate() {
                    //onSelect
                }, false,
                delegate() {
                    switch (Display) {
                        case SpeedrunDisplayMode.Current:
                            return "CURRENT ONLY";
                        case SpeedrunDisplayMode.CurrentPerRoom:
                            return "CURRENT + PER ROOM";
                        case SpeedrunDisplayMode.LiveSplit:
                            return "LIVESPLIT (" + LiveSplitPort + ")";
                    }
                    return "UNKNOWN";
                },
                delegate(string lastValue, int change) {
                    int val = (int) Display + change;
                    if (val < (int) SpeedrunDisplayMode.Current) {
                        val = (int) SpeedrunDisplayMode.Current;
                    }
                    if (val > (int) SpeedrunDisplayMode.LiveSplit) {
                        val = (int) SpeedrunDisplayMode.LiveSplit;
                    }
                    Display = (SpeedrunDisplayMode) val;
                    
                }
            );
            displayMode.UpperCase = true;
            
            toolAssist = Menu.AddItem<string>("ToolAssist", delegate() {
                    //onSelect
                }, false,
                () => (ToolAssistedSpeedrun) ? (BOTEnabled ? "BOT" : "ON") : "OFF",
                delegate(string lastValue, int change) {
                    if (change != 0) {
                        if ((!ToolAssistedSpeedrun && !BOTEnabled && change < 0) || (ToolAssistedSpeedrun && BOTEnabled && 0 < change)) {
                            //no-op
                        } else if (!ToolAssistedSpeedrun && !BOTEnabled && 0 < change) {
                            //OFF > ON
                            ToolAssistedSpeedrun = true;
                            BOTEnabled = false;
                        } else if (ToolAssistedSpeedrun && !BOTEnabled && 0 < change) {
                            //ON > BOT
                            ToolAssistedSpeedrun = true;
                            BOTEnabled = true;
                        } else if (ToolAssistedSpeedrun && BOTEnabled && change < 0) {
                            //BOT > ON
                            ToolAssistedSpeedrun = true;
                            BOTEnabled = false;
                        } else if (ToolAssistedSpeedrun && !BOTEnabled && change < 0) {
                            //ON > OFF
                            ToolAssistedSpeedrun = false;
                            BOTEnabled = false;
                        }
                    }
                    
                    FEZMod.EnableFEZometric = ToolAssistedSpeedrun;
                    FEZMod.EnableQuickWarp = ToolAssistedSpeedrun;
                    //Currently reqired unless someone hooks MovingGroupsHost and others to give a public instance
                    FEZModEngine.GetComponentsAsServices = ToolAssistedSpeedrun;
                    FEZModEngine.HandleComponents = ToolAssistedSpeedrun;
                }
            );
            toolAssist.UpperCase = true;
            
            displayMode.Selectable = toolAssist.Selectable = SpeedrunMode;
            displayMode.Disabled = toolAssist.Disabled = !SpeedrunMode;
            
            FEZMod.Menu.AddItem("FEZSpeedrunMenu", delegate() {
                mb.ChangeMenuLevel(Menu);
            });
            mb.MenuLevels.Add(Menu);
        }
        
        public static void StartClock() {
            if (!SpeedrunMode) {
                return;
            }
            
            //dispose old clock
            if (Clock != null && Clock.Running) {
                Clock.Running = false;
                Clock.Dispose();
            }
            
            switch (Display) {
                case SpeedrunDisplayMode.Current:
                case SpeedrunDisplayMode.CurrentPerRoom:
                    Clock = new SpeedrunClock();
                    break;
                case SpeedrunDisplayMode.LiveSplit:
                    LiveSplitClock lsClock = new LiveSplitClock("localhost", LiveSplitPort);
                    lsClock.Clock = new SpeedrunClock();
                    Clock = lsClock;
                    break;
            }
            
            Clock.Running = true;
            //Initialize BOT if needed
            if (BOTEnabled) {
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

