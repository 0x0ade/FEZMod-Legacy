using System;
using System.Collections.Generic;
using FezGame.Mod;

namespace FezGame.Structure {
    public class patch_SaveData : SaveData {

        public void orig_Clear() {
        }

        public void Clear() {
            orig_Clear();
            FEZMod.SaveClear(this);
        }

        public void orig_CloneInto(SaveData d) {
        }

        public void CloneInto(SaveData d) {
            orig_CloneInto(d);
            FEZMod.SaveClone(this, d);
        }

    }
}

