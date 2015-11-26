#pragma warning disable 436
using System;
using FezGame.Mod;
using System.Collections.Generic;
using System.IO;
using FezEngine.Structure;
using FezEngine.Services;
using FezEngine.Mod;

namespace FezEngine.Tools {
    public class MemoryContentManager {

        private static IEnumerable<string> assetNames;
        public static IEnumerable<string> get_AssetNames() {
            if (!FezEngineMod.CacheDisabled) {
                return MemoryContentManager.cachedAssets.Keys;
            } else {
                if (assetNames == null) {
                    List<string> files = TraverseThrough("Resources");
                    List<string> assets = new List<string>(files.Count);
                    for (int i = 0; i < files.Count; i++) {
                        string file = files[i];
                        assets.Add(file.Substring(10, file.Length-14).Replace("/", "\\"));
                    }
                    assetNames = assets;
                }
                return assetNames;
            }
        }

        private static List<string> TraverseThrough(string dir, List<string> list = null) {
            if (!Directory.Exists(dir)) {
                return list;
            }
            
            if (list == null) {
                list = new List<string>();
            }

            string[] dirs = Directory.GetDirectories(dir);
            for (int i = 0; i < dirs.Length; i++) {
                list = TraverseThrough(dirs[i], list);
            }
            
            string[] files = Directory.GetFiles(dir);
            for (int i = 0; i < files.Length; i++) {
                list.Add(files[i]);
            }

            return list;
        }

        private static Dictionary<String, byte[]> cachedAssets;

        private static int DumpAllResourcesCount = 0;

        public static void DumpAll() {
            if (cachedAssets == null) {
                ModLogger.Log("FEZMod.Engine", "Cached assets do not exist; ignoring...");
                return;
            }

            int dumped = 0;

            int count = cachedAssets.Count;
            if (count <= DumpAllResourcesCount) {
                return;
            }
            DumpAllResourcesCount = count;
            ModLogger.Log("FEZMod.Engine", "Dumping "+count+" assets...");
            String[] assetNames_ = new String[count];
            cachedAssets.Keys.CopyTo(assetNames_, 0);

            for (int i = 0; i < count; i++) {
                byte[] bytes = cachedAssets[assetNames_[i]];
                string assetName = assetNames_[i].ToLower();
                string extension = ".xnb";
                #if FNA
                //The FEZ 1.12 .pak files store the raw fxb files
                if (assetName.StartsWith("effects")) {
                    extension = ".fxb";
                }
                #endif
                string filePath = assetName.Externalize() + extension;
                FileInfo file = new FileInfo(filePath);
                if (!file.Exists) {
                    file.Directory.Create();
                    ModLogger.Log("FEZMod.Engine", (i+1)+" / "+count+": "+assetName+" -> "+filePath);
                    FileStream fos = new FileStream(filePath, FileMode.CreateNew);
                    fos.Write(bytes, 0, bytes.Length);
                    fos.Close();
                    dumped++;
                }
            }

            ModLogger.Log("FEZMod.Engine", "Dumped: "+dumped+" / "+count);
        }

        protected extern Stream orig_OpenStream(string assetName);
        protected Stream OpenStream(string assetName) {
            if (FezEngineMod.DumpAllResources) {
                DumpAll();
            }

            string extension = ".xnb";
            #if FNA
            //The FEZ 1.12 .pak files store the raw fxb files, which are dumped as fxbs
            if (assetName.ToLower().StartsWith("effects")) {
                extension = ".fxb";
            }
            #endif
            string filePath = assetName.Externalize() + extension;
            FileInfo file = new FileInfo(filePath);
            if (file.Exists) {
                FileStream fis = new FileStream(filePath, FileMode.Open);
                return fis;
            } else if (FezEngineMod.DumpResources) {
                file.Directory.Create();
                ModLogger.Log("FEZMod.Engine", assetName+" -> "+filePath);
                Stream ois = orig_OpenStream(assetName);
                FileStream fos = new FileStream(filePath, FileMode.CreateNew);
                ois.CopyTo(fos);
                ois.Close();
                fos.Close();
            }
            return orig_OpenStream(assetName);
        }

        public static extern bool orig_AssetExists(string assetName);
        public static bool AssetExists(string assetName) {
            string assetPath = assetName.Externalize();
            if (File.Exists(assetPath + ".xnb") ||
                File.Exists(assetPath + ".fxb") ||
                File.Exists(assetPath + ".ogg") ||
                File.Exists(assetPath + ".png") ||
                File.Exists(assetPath + ".jpg") ||
                File.Exists(assetPath + ".jpeg") ||
                File.Exists(assetPath + ".gif")) {
                return true;
            }

            return orig_AssetExists(assetName);
        }

        public extern void orig_LoadEssentials();
        public void LoadEssentials() {
            if (!FezEngineMod.CacheDisabled) {
                orig_LoadEssentials();
            } else {
                cachedAssets = new Dictionary<string, byte[]>(0);
            }
            FEZMod.LoadEssentials();
        }

        public extern void orig_Preload();
        public void Preload() {
            if (!FezEngineMod.CacheDisabled) {
                orig_Preload();
            }
            FEZMod.Preload();
        }

    }
}

