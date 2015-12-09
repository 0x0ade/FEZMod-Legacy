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
        public static bool LiveSplitSync = false;
        public static int LiveSplitPort = 16834;
        public static bool PerRoomTime = false;

        public static bool ToolAssistedSpeedrun = false;
        public static bool BOTEnabled = false;//Broken Optimizing Thing

        public static ISpeedrunClock Clock = new SpeedrunClock();

        public static List<SplitCase> DefaultSplitCases = new List<SplitCase>();
        
        public static MenuLevel Menu;

        public FezSpeedrun() {
        }

        public override void ParseArgs(string[] args) {
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-sr" || args[i] == "--speedrun") {
                    ModLogger.Log("FEZMod", "Found -sr / --speedrun");
                    if (i + 1 < args.Length && !args[i+1].StartsWith("-")) {
                        ModLogger.Log("FEZMod", "Connecting to LiveSplit on port " + args[i + 1] + "...");
                        LiveSplitClock lsClock = new LiveSplitClock("localhost", int.Parse(args[i + 1]));
                        lsClock.Clock = Clock;
                        Clock = lsClock;
                    }
                    SpeedrunMode = true;
                    FEZMod.EnableFEZometric = false;
                    FEZMod.EnableQuickWarp = false;
                    FEZMod.EnableBugfixes = false;
                }
                if (Clock is LiveSplitClock && (args[i] == "-lss" || args[i] == "--livesplit-sync")) {
                    ModLogger.Log("FEZMod", "Found -lss / --livesplit-sync");
                    ((LiveSplitClock) Clock).Sync = true;
                }
                if (Clock != null && (args[i] == "-prt" || args[i] == "--per-room-time")) {
                    ModLogger.Log("FEZMod", "Found -prt / --per-room-time");
                    PerRoomTime = true;
                }
                if (Clock != null && (args[i] == "-tas" || args[i] == "--tool-assisted-speedrun")) {
                    ModLogger.Log("FEZMod", "Found -tas / --tool-assisted-speedrun");
                    ToolAssistedSpeedrun = true;
                    FEZMod.EnableFEZometric = true;
                    FEZMod.EnableQuickWarp = true;
                    //Currently reqired unless someone hooks MovingGroupsHost and others to give a public instance
                    FEZModEngine.GetComponentsAsServices = true;
                    FEZModEngine.HandleComponents = true;
                }
                if (Clock != null && (args[i] == "-bot" || args[i] == "--bot")) {
                    ModLogger.Log("FEZMod", "Found -bot / --bot");
                    BOTEnabled = true;
                }
            }
            
            TextPatchHelper.Static.Fallback["FEZSpeedrunMenu"] = "SPEEDRUN MENU";
            TextPatchHelper.Static.Fallback["SpeedrunMode"] = "Mode: {0}";
            TextPatchHelper.Static.Fallback["DisplayMode"] = "Display time: {0}";
            TextPatchHelper.Static.Fallback["ToolAssist"] = "Tool assist (quicksaves, ...): {0}";
        }

        public override void LoadComponents(Fez game) {
            if (SpeedrunMode) {
                ServiceHelper.AddComponent(new SpeedrunInfo(ServiceHelper.Game));
            }
            if (ToolAssistedSpeedrun) {
                ServiceHelper.AddComponent(new TASComponent(ServiceHelper.Game));
            }
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
                    if (LiveSplitSync && mb.selectorPhase == SelectorPhase.Select) {
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
                    SpeedrunMode = !SpeedrunMode;
                    displayMode.Selectable = toolAssist.Selectable = SpeedrunMode;
                    displayMode.Disabled = toolAssist.Disabled = !SpeedrunMode;
                }
            );
            item.UpperCase = true;
            
            displayMode = Menu.AddItem<string>("DisplayMode", delegate() {
                    //onSelect
                }, false,
                () => 
                !PerRoomTime && !LiveSplitSync ? "CURRENT ONLY" : 
                 PerRoomTime && !LiveSplitSync ? "CURRENT + PER ROOM" :
                !PerRoomTime && LiveSplitSync ? "LIVESPLIT (" + LiveSplitPort + ")" :
                "UNKNOWN",
                delegate(string lastValue, int change) {
                    if (
                        //well... keep as-is?
                        change == 0 ||
                        (change < 0 && !PerRoomTime && !LiveSplitSync) ||
                        (0 < change && !PerRoomTime && LiveSplitSync)
                    ) {
                        return;
                    }
                    
                    if (
                        //CURRENT + PER ROOM -> CURRENT ONLY
                        (change < 0 && PerRoomTime && !LiveSplitSync)
                    ) {
                        PerRoomTime = false;
                        LiveSplitSync = false;
                    } else if (
                        //CURRENT ONLY -> CURRENT + PER ROOM
                        (change < 0 && !PerRoomTime && LiveSplitSync) ||
                        //LIVESPLIT -> CURRENT + PER ROOM
                        (0 < change && !PerRoomTime && !LiveSplitSync)
                    ) {
                        PerRoomTime = true;
                        LiveSplitSync = false;
                        return;
                    } else if (
                        //CURRENT + PER ROOM -> LIVESPLIT
                        (0 < change && PerRoomTime && !LiveSplitSync)
                    ) {
                        PerRoomTime = false;
                        LiveSplitSync = true;
                    } else {
                        //RESET DUE TO ERROR
                        PerRoomTime = false;
                        LiveSplitSync = false;
                    }
                    
                }
            );
            displayMode.UpperCase = true;
            
            toolAssist = Menu.AddItem<string>("ToolAssist", delegate() {
                    //onSelect
                }, false,
                () => (ToolAssistedSpeedrun) ? StaticText.GetString("On") : StaticText.GetString("Off"),
                delegate(string lastValue, int change) {
                    ToolAssistedSpeedrun = !ToolAssistedSpeedrun;
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
        
        public static void TriggerClock() {
            if (!SpeedrunMode) {
                return;
            }
            
            //TODO initialize the clock here
            
            if (Clock.Running) {
                Clock.Running = false; //Forces the clock to reset.
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
            }
        }

    }
}

