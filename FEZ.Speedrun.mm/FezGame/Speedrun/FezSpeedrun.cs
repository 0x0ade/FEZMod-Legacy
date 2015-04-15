using System;
using FezGame.Mod;
using Common;
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
        public override string Author { get { return "AngelDE98 & JAFM contributors"; } }
        public override string Version { get { return FEZMod.Version; } }

        public static bool SpeedrunMode = false;

        public static ISpeedrunClock Clock = new SpeedrunClock();

        public static TcpClient LiveSplitClient;
        public static NetworkStream LiveSplitStream;
        public static bool LiveSplitSync = false;

        public static List<SplitCase> DefaultSplitCases = new List<SplitCase>();

        public FezSpeedrun() {
        }

        public override void ParseArgs(string[] args) {
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-sr" || args[i] == "--speedrun") {
                    ModLogger.Log("JAFM", "Found -sr / --speedrun");
                    if (i + 1 < args.Length && !args[i+1].StartsWith("-")) {
                        if (args[i + 1] != "strict") {
                            ModLogger.Log("JAFM", "Connecting to LiveSplit on port " + args[i + 1] + "...");
                            //Clock = new LiveSplitClock("localhost", int.Parse(args[i + 1]));
                            LiveSplitClient = new TcpClient("localhost", int.Parse(args[i + 1]));
                            LiveSplitStream = FezSpeedrun.LiveSplitClient.GetStream();
                        } else {
                            ModLogger.Log("JAFM", "Switching to strict mode...");
                            Clock.Strict = true;
                        }
                    }
                    SpeedrunMode = true;
                    FEZMod.EnableFEZometric = false;
                    FEZMod.EnableQuickWarp = false;
                    FEZMod.EnableBugfixes = false;
                }
                if (args[i] == "-lss" || args[i] == "--livesplit-sync") {
                    ModLogger.Log("JAFM", "Found -lss / --livesplit-sync");
                    LiveSplitSync = true;
                }
            }
        }

        public override void LoadComponents(Fez game) {
            if (SpeedrunMode) {
                ServiceHelper.AddComponent(new SpeedrunInfo(ServiceHelper.Game));
            }
        }

        public override void Exit() {
            if (LiveSplitClient != null) {
                LiveSplitStream.Close();
                LiveSplitClient.Close();
            }
        }

    }
}

