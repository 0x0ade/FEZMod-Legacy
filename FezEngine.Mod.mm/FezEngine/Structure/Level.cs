using System;

namespace FezEngine.Structure {
    public class Level {

        public static bool IsNoFlat = false;

        public bool orig_get_Flat() {
            return false;
        }

        public bool get_Flat() {
            return orig_get_Flat() || !IsNoFlat;
        }

    }
}

