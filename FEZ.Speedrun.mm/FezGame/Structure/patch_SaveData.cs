using System;
using System.Collections.Generic;
using FezGame.Mod;

namespace FezGame.Structure {
    public class patch_SaveData : SaveData {

        public Dictionary<string, TimeSpan> LevelTimes = new Dictionary<string, TimeSpan>();
        public TimeSpan Time = new TimeSpan();

    }
}

