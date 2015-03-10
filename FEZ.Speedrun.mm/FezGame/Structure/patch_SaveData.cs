using System;
using System.Collections.Generic;
using FezGame.Mod;
using FezGame.Speedrun;

namespace FezGame.Structure {
    public class patch_SaveData : SaveData {

        public List<Split> LevelTimes = new List<Split>();
        public TimeSpan Time = new TimeSpan();
        public TimeSpan TimeLoading = new TimeSpan();

    }
}

