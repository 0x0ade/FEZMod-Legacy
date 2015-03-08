﻿using System;
using FezGame.Mod;
using Common;
using FezEngine.Tools;
using FezGame.Components;
using Microsoft.Xna.Framework;
using FezGame.Structure;
using System.Collections.Generic;

namespace FezGame.Speedrun {
    public class FezSpeedrun : FezModule {

        public override string Name { get { return "JAFM.Speedrun"; } }
        public override string Author { get { return "AngelDE98 & JAFM contributors"; } }
        public override string Version { get { return FEZMod.Version; } }

        public static bool SpeedrunMode = false;

        public FezSpeedrun() {
        }

        public override void ParseArgs(string[] args) {
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-sr" || args[i] == "--speedrun") {
                    ModLogger.Log("JAFM", "Found -sr / --speedrun");
                    SpeedrunMode = true;
                    FEZMod.EnableFEZometric = false;
                    FEZMod.EnableQuickWarp = false;
                }
            }
        }

        public override void Initialize() {
            if (SpeedrunMode) {
                ServiceHelper.AddComponent(new SpeedrunInfo(ServiceHelper.Game));
            }
        }

        public override void SaveClear(SaveData saveData) {
            saveData.Set("LevelTimes", new Dictionary<string, TimeSpan>());
            saveData.Set("Time", new TimeSpan());
        }

        public override void SaveClone(SaveData source, SaveData dest) {
            Dictionary<string, TimeSpan> sourceLevelTimes = source.Get<Dictionary<string, TimeSpan>>("LevelTimes");
            Dictionary<string, TimeSpan> destLevelTimes = dest.Get<Dictionary<string, TimeSpan>>("LevelTimes");

            destLevelTimes.Clear();
            foreach (string level in sourceLevelTimes.Keys) {
                destLevelTimes.Add(level, sourceLevelTimes[level]);
            }
            dest.Set("LevelTimes", destLevelTimes);

            dest.Set("Time", source.Get<TimeSpan>("Time"));
        }

    }
}
