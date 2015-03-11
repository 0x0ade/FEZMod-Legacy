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

namespace FezGame.Speedrun {
    public class FezSpeedrun : FezModule {

        public override string Name { get { return "JAFM.Speedrun"; } }
        public override string Author { get { return "AngelDE98 & JAFM contributors"; } }
        public override string Version { get { return FEZMod.Version; } }

        public static bool SpeedrunMode = false;
        public static bool SpeedrunList = false;
        public static bool Strict = false;

        public static TcpClient LiveSplitClient;
        public static NetworkStream LiveSplitStream;
        public static bool LiveSplitSync = false;

        public FezSpeedrun() {
        }

        public override void ParseArgs(string[] args) {
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-sr" || args[i] == "--speedrun") {
                    ModLogger.Log("JAFM", "Found -sr / --speedrun");
                    if (i + 1 < args.Length && !args[i+1].StartsWith("-")) {
                        if (args[i + 1] != "strict") {
                            ModLogger.Log("JAFM", "Connecting to LiveSplit on port " + args[i + 1] + "...");
                            LiveSplitClient = new TcpClient("localhost", int.Parse(args[i + 1]));
                            LiveSplitStream = FezSpeedrun.LiveSplitClient.GetStream();
                            Strict = true;
                        } else {
                            ModLogger.Log("JAFM", "Switching to strict mode...");
                            Strict = true;
                        }
                    }
                    SpeedrunMode = true;
                    FEZMod.EnableFEZometric = false;
                    FEZMod.EnableQuickWarp = false;
                }
                if (args[i] == "-sl" || args[i] == "--split-list") {
                    ModLogger.Log("JAFM", "Found -sl / --split-list");
                    SpeedrunList = true;
                }
                if (args[i] == "-ls" || args[i] == "--livesplit-sync") {
                    ModLogger.Log("JAFM", "Found -ls / --livesplit-sync");
                    LiveSplitSync = true;
                }
            }
        }

        public override void Initialize() {
            if (SpeedrunMode) {
                ServiceHelper.AddComponent(new SpeedrunInfo(ServiceHelper.Game));
            }
        }

        public override void SaveClear(SaveData saveData) {
            saveData.Set("LevelTimes", new List<Split>());
            saveData.Set("Time", new TimeSpan());
            saveData.Set("TimeLoading", new TimeSpan());
        }

        public override void SaveClone(SaveData source, SaveData dest) {
            List<Split> sourceLevelTimes = source.Get<List<Split>>("LevelTimes");
            List<Split> destLevelTimes = dest.Get<List<Split>>("LevelTimes");

            destLevelTimes.Clear();
            foreach (Split levelTime in sourceLevelTimes) {
                destLevelTimes.Add(levelTime);
            }
            dest.Set("LevelTimes", destLevelTimes);

            dest.Set("Time", source.Get<TimeSpan>("Time"));
            dest.Set("TimeLoading", source.Get<TimeSpan>("TimeLoading"));
        }

        public override void SaveRead(SaveData saveData, CrcReader reader) {
            List<Split> levelTimes = new List<Split>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++) {
                levelTimes.Add(new Split(reader.ReadString(), reader.ReadTimeSpan()));
            }
            saveData.Set("LevelTimes", levelTimes);

            saveData.Set("Time", reader.ReadTimeSpan());
            saveData.Set("TimeLoading", reader.ReadTimeSpan());
        }

        public override void SaveWrite(SaveData saveData, CrcWriter writer) {
            List<Split> levelTimes = saveData.Get<List<Split>>("LevelTimes");
            writer.Write(levelTimes.Count);
            foreach (Split levelTime in levelTimes) {
                writer.Write(levelTime.Level);
                writer.Write(levelTime.Time);
            }

            writer.Write(saveData.Get<TimeSpan>("Time"));
            writer.Write(saveData.Get<TimeSpan>("TimeLoading"));
        }

    }
}

