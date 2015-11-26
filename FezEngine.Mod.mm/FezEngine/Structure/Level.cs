using FezEngine.Mod;

namespace FezEngine.Structure {
    public class Level {

        public extern bool orig_get_Flat();
        public bool get_Flat() {
            return orig_get_Flat() && !FezEngineMod.FlatDisabled;
        }

    }
}

