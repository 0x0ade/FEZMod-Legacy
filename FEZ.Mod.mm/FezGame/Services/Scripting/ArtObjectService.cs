using System;
using FezGame.Mod;
using FezEngine.Mod;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezEngine.Components.Scripting;
using FezGame.Structure;
using FezGame.Components;
using Microsoft.Xna.Framework;
using System.IO;
using MonoMod;

namespace FezGame.Services.Scripting {
    public class ArtObjectService {
        
        public readonly static Color HexahedronColorBG = new Color(255, 231, 109); //brightest yellow
        public readonly static Color HexahedronColorFG = new Color(197, 145, 29); //darker than darkest yellow (247, 195, 49)
        
        public ISpeechBubbleManager SpeechBubble { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IGameLevelManager LevelManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        
        public extern LongRunningAction orig_Say(int id, string text, bool zuish);
        public LongRunningAction Say(int id, string text, bool zuish) {
            patch_ISpeechBubbleManager speechBubbleEXT = (patch_ISpeechBubbleManager) SpeechBubble;
            speechBubbleEXT.ColorBG = Color.Black;
            speechBubbleEXT.ColorFG = Color.White;
            
            ArtObjectInstance ao;
            if (!FEZModEngine.Settings.ModdedSpeechBubbles || !LevelManager.ArtObjects.TryGetValue(id, out ao)) {
                return orig_Say(id, text, zuish);
            }
            
            string name = ao.ArtObjectName.ToLowerInvariant();
            if (name.EndsWith("_1ao")) {
                name = name.Substring(0, name.Length - 4);
            } else if (name.EndsWith("ao")) {
                name = name.Substring(0, name.Length - 2);
            }
            
            if (name.Contains("hexahedron") || name == "new_hex") {
                name = "hexahedron";
                speechBubbleEXT.ColorBG = HexahedronColorBG;
                speechBubbleEXT.ColorFG = HexahedronColorFG;
                speechBubbleEXT.ColorSecondaryFG = HexahedronColorFG;
            }
            
            speechBubbleEXT.Speaker = name.Replace("_", " ");
            
            LongRunningAction action = orig_Say(id, text, zuish);
            return action;
        }
        
    }
}
