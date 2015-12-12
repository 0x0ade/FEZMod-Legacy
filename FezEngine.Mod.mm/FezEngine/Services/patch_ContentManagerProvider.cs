using FezEngine.Tools;
using FezEngine.Mod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMod;

namespace FezEngine.Services {
    public class patch_ContentManagerProvider {
        
        [MonoModIgnore]
        private readonly Dictionary<string, SharedContentManager> levelScope;
        
        private readonly List<string> levelScopeToRemove = new List<string>();
        private readonly List<string> temporaryToRemove = new List<string>();
        
        private readonly List<string> precaching = new List<string>();
        
        public ILevelManager LevelManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        
        private extern void orig_CleanAndPrecache();
        private void CleanAndPrecache() {
            if (FEZModEngine.Settings.DataCache != DataCacheMode.Smart) {
                orig_CleanAndPrecache();
                return;
            }
            
            precaching.Clear();
            
            IEnumerable<string> linked = LevelManager.LinkedLevels();
            
            foreach (string key in levelScope.Keys) {
                if (key != LevelManager.Name && !linked.Contains(key)) {
                    levelScope[key].Dispose();
                    levelScopeToRemove.Add(key);
                }
            }
            foreach (string key in levelScopeToRemove) {
                levelScope.Remove(key);
            }
            levelScopeToRemove.Clear();
            
            foreach (KeyValuePair<string, CachedAssetData> pair in AssetDataCache.Temporary) {
                if (pair.Value.References <= 1) {
                    pair.Value.Age++;
                }
                
                if (pair.Value.Age > 2) {
                    temporaryToRemove.Add(pair.Key);
                }
            }
            foreach (string key in temporaryToRemove) {
                AssetDataCache.Temporary.Remove(key);
            }
            temporaryToRemove.Clear();
            
            AssetDataCache.Preloaded.Clear();
            //TODO schedule preloads for AssetDataCache.Preloaded
        }
        
    }
}