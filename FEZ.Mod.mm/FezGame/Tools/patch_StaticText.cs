﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using FezEngine.Tools;
using FezEngine.Services;

namespace FezGame.Tools {
    public static class patch_StaticText {
        public static string orig_GetString(string tag) {
            return null;
        }

        public static string GetString(string tag) {
            string str;
            if (orig_TryGetString(tag, out str)) {
                return str;
            }
            return "[S:"+tag+"]";
        }
        
        public static bool orig_TryGetString(string tag, out string text) {
            text = null;
            return true;
        }

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
