namespace FezGame.Tools {
    public static class patch_GameText {
        private const string MISSING_TEXT = "[MISSING TEXT]";
        
        public static extern string orig_GetString(string tag);
        public static string GetString(string tag) {
            string str = TextPatchHelper.Game.Get(tag);
            if (str != null) {
                return str;
            }
            str = orig_GetString(tag);
            if (str != MISSING_TEXT) {
                return str;
            }
            return "[G:"+tag+"]";
        }
        
        public static extern string orig_GetStringRaw(string tag);
        public static string GetStringRaw(string tag) {
            string str;
            if (TextPatchHelper.Game.Fallback.TryGetValue(tag, out str)) {
                return str;
            }
            str = orig_GetStringRaw(tag);
            if (str != MISSING_TEXT) {
                return str;
            }
            return "[GR:"+tag+"]";
        }
    }
}
