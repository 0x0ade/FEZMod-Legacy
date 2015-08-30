using System;
using FezGame.Mod;
using FezGame.Mod;
using FezEngine.Tools;
using FezGame.Components;
using Microsoft.Xna.Framework;
using FezGame.Structure;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using FezGame.Speedrun.Clocks;
using FezEngine.Structure;
using System.Threading;

namespace FezGame.Speedrun {
    public class FezSpeedrun : FezModule {

        public override string Name { get { return "JAFM.Speedrun"; } }
        public override string Author { get { return "AngelDE98 & FEZMod contributors"; } }
        public override string Version { get { return FEZMod.Version; } }

        public static bool SpeedrunMode = false;
        public static bool PerRoomTime = false;

        public static bool ToolAssistedSpeedrun = false;

        public static ISpeedrunClock Clock = new SpeedrunClock();

        public static List<SplitCase> DefaultSplitCases = new List<SplitCase>();

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
                }
            }
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

    }
}

