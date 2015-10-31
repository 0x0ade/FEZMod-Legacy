namespace FezGame.Tools {
    public static class patch_GameText {
        private const string MISSING_TEXT = "[MISSING TEXT]";
        
        public static string orig_GetString(string tag) {
            return null;
        }

        public static string GetString(string tag) {
            string str = orig_GetString(tag);
            if (str != MISSING_TEXT) {
                return str;
            }
            return "[G:"+tag+"]";
        }
        
        public static string orig_GetStringRaw(string tag) {
            return null;
        }
        
        public static string GetStringRaw(string tag) {
            string str = orig_GetStringRaw(tag);
            if (str != MISSING_TEXT) {
                return str;
            }
            return "[GR:"+tag+"]";
        }
    }
}
