namespace FezEngine.Structure {
    public class Level {

        public static bool FlatDisabled = false;

        public extern bool orig_get_Flat();
        public bool get_Flat() {
            return orig_get_Flat() && !FlatDisabled;
        }

    }
}

