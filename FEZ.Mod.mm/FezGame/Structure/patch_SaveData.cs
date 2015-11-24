using FezGame.Mod;

namespace FezGame.Structure {
    public class patch_SaveData : SaveData {

        public extern void orig_Clear();
        public new void Clear() {
            orig_Clear();
            FEZMod.SaveClear(this);
        }

        public extern void orig_CloneInto(SaveData d);
        public new void CloneInto(SaveData d) {
            orig_CloneInto(d);
            FEZMod.SaveClone(this, d);
        }

    }
}

