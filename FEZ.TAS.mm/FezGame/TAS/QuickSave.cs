using System;
using FezGame.Structure;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace FezGame.TAS {
    public class QuickSave {

        public SaveData SaveData = new SaveData();
        public Texture2D Thumbnail;

        public TimeSpan Time = new TimeSpan(0);
        public TimeSpan TimeLoading = new TimeSpan(0);

        public List<CacheKey_Info_Value[]> RewindData = new List<CacheKey_Info_Value[]>();

    }
}

