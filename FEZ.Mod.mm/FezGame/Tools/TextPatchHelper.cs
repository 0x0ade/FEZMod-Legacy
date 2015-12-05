using System;
using System.Collections.Generic;
using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework.Content;

namespace FezGame.Tools {
    
    public class TextType {
        public string Name;
        public Dictionary<string, Dictionary<string, string>> Map;
        public Dictionary<string, string> Fallback;
        
        public TextType()
            : this("UNKNOWN") {
        }
        
        public TextType(string name) {
            Name = name;
        }
        
        public void Load() {
            try {
                Map = ServiceHelper.Get<IContentManagerProvider>().Global.
                    Load<Dictionary<string, Dictionary<string, string>>>("Texts/" + Name);
                Fallback = Map[string.Empty];
            } catch (Exception e) {
                //loading failed - fall back to empty maps
                Map = new Dictionary<string, Dictionary<string, string>>();
                Fallback = Map[string.Empty] = new Dictionary<string, string>();
            }
        }
        
        public string Get(string tag) {
            if (tag == null) {
                return null;
            }
            
			Dictionary<string, string> map;
			if (!Map.TryGetValue(Culture.TwoLetterISOLanguageName, out map)) {
				map = Fallback;
			}
            
			string str;
			if ((!map.TryGetValue(tag, out str)) && (!Fallback.TryGetValue(tag, out str))) {
				return null;
			}
			return str;
        }
    }
    
    public static class TextPatchHelper {
        
        public readonly static TextType Game = new TextType("game");
        public readonly static TextType Static = new TextType("static");
        
        static TextPatchHelper() {
            Game.Load();
            Static.Load();
		}
        
    }
}