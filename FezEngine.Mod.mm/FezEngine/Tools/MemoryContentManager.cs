using System;
using Common;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using FezEngine.Structure;

namespace FezEngine.Tools {
    public class MemoryContentManager {

        private static IEnumerable<string> assetNames;
        public static IEnumerable<string> get_AssetNames() {
            if (!CacheDisabled) {
                return MemoryContentManager.cachedAssets.Keys;
            } else {
                if (assetNames == null) {
                    List<string> files = TraverseThrough("Resources");
                    List<string> assets = new List<string>(files.Count);
                    foreach (string file in files) {
                        assets.Add(file.Substring(10, file.Length-14).Replace("/", "\\"));
                    }
                    assetNames = assets;
                }
                return assetNames;
            }
        }

        private static List<string> TraverseThrough(string dir) {
            return TraverseThrough(new List<string>(), dir);
        }

        private static List<string> TraverseThrough(List<string> list, string dir) {
            if (!Directory.Exists(dir)) {
                return list;
            }

            foreach (string subdir in Directory.GetDirectories(dir)) {
                list = TraverseThrough(list, subdir);
            }

            foreach (string file in Directory.GetFiles(dir)) {
                list.Add(file);
            }

            return list;
        }

        private static Dictionary<String, byte[]> cachedAssets;

        public static bool DumpResources = false;
        public static bool DumpAllResources = false;
        private static int DumpAllResourcesCount = 0;

        public static bool CacheDisabled = false;

        public void DumpAll() {
            if (cachedAssets == null) {
                ModLogger.Log("JAFM.ResourceMod", "Cached assets do not exist; ignoring...");
            }

            int dumped = 0;

            int count = cachedAssets.Count;
            if (count <= DumpAllResourcesCount) {
                return;
            }
            DumpAllResourcesCount = count;
            ModLogger.Log("JAFM.ResourceMod", "Dumping "+count+" assets...");
            String[] assetNames = new String[count];
            cachedAssets.Keys.CopyTo(assetNames, 0);

            for (int i = 0; i < count; i++) {
                byte[] bytes = cachedAssets[assetNames[i]];
                string assetName = assetNames[i].ToLower();
                string filePath = ("Resources\\"+(assetName.ToLower())).Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString())+".xnb";
                FileInfo file = new FileInfo(filePath);
                if (!file.Exists) {
                    file.Directory.Create();
                    ModLogger.Log("JAFM.ResourceMod", (i+1)+" / "+count+": "+assetName+" -> "+filePath);
                    FileStream fos = new FileStream(filePath, FileMode.CreateNew);
                    fos.Write(bytes, 0, bytes.Length);
                    fos.Close();
                    dumped++;
                }
            }

            ModLogger.Log("JAFM.ResourceMod", "Dumped: "+dumped+" / "+count);
        }

        protected Stream orig_OpenStream(String assetName) {
            return null;
        }

        protected Stream OpenStream(String assetName) {
            if (DumpAllResources) {
                DumpAll();
            }

            string filePath = ("Resources\\"+(assetName.ToLower())).Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString())+".xnb";
            FileInfo file = new FileInfo(filePath);
            if (file.Exists) {
                FileStream fis = new FileStream(filePath, FileMode.Open);
                return fis;
            } else if (DumpResources) {
                file.Directory.Create();
                ModLogger.Log("JAFM.ResourceMod", assetName+" -> "+filePath);
                Stream ois = orig_OpenStream(assetName);
                FileStream fos = new FileStream(filePath, FileMode.CreateNew);
                ois.CopyTo(fos);
                ois.Close();
                fos.Close();
            }
            return orig_OpenStream(assetName);
        }

        public static bool orig_AssetExists(String assetName) {
            return false;
        }

        public static bool AssetExists(String assetName) {
            if (assetName == "JAFM_DUMP_WORKAROUND") {
                DumpResources = true;
                return true;
            }
            if (assetName == "JAFM_DUMPALL_WORKAROUND") {
                DumpAllResources = true;
                return true;
            }
            if (assetName == "JAFM_NOFLAT_WORKAROUND") {
                Level.FlatDisabled = true;
                return true;
            }
            if (assetName == "JAFM_NOCACHE_WORKAROUND") {
                CacheDisabled = true;
                return true;
            }

            string filePath = ("Resources\\"+(assetName.ToLower())).Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString())+".xnb";
            FileInfo file = new FileInfo(filePath);
            if (file.Exists) {
                return true;
            }

            return orig_AssetExists(assetName);
        }

        public void orig_LoadEssentials() {
        }

        public void LoadEssentials() {
            if (!CacheDisabled) {
                orig_LoadEssentials();
            } else {
                cachedAssets = new Dictionary<string, byte[]>(0);
            }
        }

        public void orig_Preload() {
        }

        public void Preload() {
            if (!CacheDisabled) {
                orig_Preload();
            }
        }

    }
}

