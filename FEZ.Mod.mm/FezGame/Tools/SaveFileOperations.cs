using System;
using FezGame.Mod;
using FezGame.Structure;
using FezEngine.Tools;

namespace FezGame.Tools {
    public class SaveFileOperations {

        public static void orig_Write(CrcWriter w, SaveData sd) {
        }

        public static void Write(CrcWriter w, SaveData sd) {
            orig_Write(w, sd);
            FEZMod.SaveWrite(sd, w);
        }

        public static SaveData orig_Read(CrcReader r) {
            return null;
        }

        public static SaveData Read(CrcReader r) {
            SaveData sd = orig_Read(r);
            r.ReadBoolean();//Write writes isNew but Read ignores it.
            FEZMod.SaveRead(sd, r);
            return sd;
        }

    }
}

