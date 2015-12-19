using System;
using FezGame.Mod;
using FezEngine.Mod;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezEngine.Components.Scripting;
using FezGame.Structure;
using FezGame.Components;
using Microsoft.Xna.Framework.Content;
using System.IO;
using MonoMod;

namespace FezGame.Services {
    public class ArtObjectService {
        
        public extern LongRunningAction orig_Say(int id, string text, bool zuish);
        public LongRunningAction Say(int id, string text, bool zuish) {
            LongRunningAction action = orig_Say(id, text, zuish);
            
            //TODO
            
            return action;
        }
        
    }
}
