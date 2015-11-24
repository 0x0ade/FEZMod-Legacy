namespace FezGame.Tools {
    public static class patch_GameText {
        private const string MISSING_TEXT = "[MISSING TEXT]";
        
        public static extern string orig_GetString(string tag);
        public static string GetString(string tag) {
            string str = orig_GetString(tag);
            if (str != MISSING_TEXT) {
                return str;
            }
            return "[G:"+tag+"]";
        }
        
        public static extern string orig_GetStringRaw(string tag);
        public static string GetStringRaw(string tag) {
            string str = orig_GetStringRaw(tag);
            if (str != MISSING_TEXT) {
                return str;
            }
            return "[GR:"+tag+"]";
        }
    }
}
