using FezGame.Mod;
using FezGame.Structure;
using FezEngine.Tools;

namespace FezGame.Tools {
    public class patch_SaveFileOperations {

        public static extern void orig_Write(CrcWriter w, SaveData sd);
        public static void Write(CrcWriter w, SaveData sd) {
            orig_Write(w, sd);
            FEZMod.SaveWrite(sd, w);
        }

        public static extern SaveData orig_Read(CrcReader r);
        public static SaveData Read(CrcReader r) {
            SaveData sd = orig_Read(r);
            r.ReadBoolean();//Write writes isNew but Read ignores it.
            //Reason why Read doesn't read isNew:
            //Renaud confirmed that isNew is not accurate enough and thus
            //causes bugs™. Instead of reading isNew, Read instead checks
            //the save data if it's new by its other values.
            FEZMod.SaveRead(sd, r);
            return sd;
        }

    }
}

