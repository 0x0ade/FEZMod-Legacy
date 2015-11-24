namespace FezGame.Tools {
    public static class patch_StaticText {
        public static extern string orig_GetString(string tag);
        public static string GetString(string tag) {
            string str;
            if (orig_TryGetString(tag, out str)) {
                return str;
            }
            return "[S:"+tag+"]";
        }
        
        public static extern bool orig_TryGetString(string tag, out string text);
        public static bool TryGetString(string tag, out string text) {
            string str;
            if (orig_TryGetString(tag, out str)) {
                text = str;
                return true;
            }
            text = "[ST:"+tag+"]";
            return false;
        }
    }
}

