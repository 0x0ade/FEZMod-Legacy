#pragma warning disable 436
using System;
using System.Collections.Generic;
using System.IO;
using FezEngine.Structure;
using FezEngine.Services;
using FezEngine.Mod;
using Microsoft.Xna.Framework.Content;
using MonoMod;
using System.Reflection;

namespace FezEngine.Tools {
    public class patch_MemoryContentManager : ContentManager {
        
        public patch_MemoryContentManager(IServiceProvider serviceProvider, string rootDirectory)
            : base(serviceProvider, rootDirectory) {
            //no-op
        }
        
        private static IEnumerable<string> assetNames;
        private static int assetNamesFromMetadata;
        private static int assetNamesFromCache;
        public static IEnumerable<string> get_AssetNames() {
            if (assetNames == null || assetNamesFromMetadata != AssetMetadata.Map.Count) {
                List<string> files = TraverseThrough("Resources");
                List<string> assets = new List<string>(files.Count + AssetMetadata.Map.Keys.Count);
                for (int i = 0; i < files.Count; i++) {
                    string file = files[i];
                    assets.Add(file.Substring(10, file.Length-14).Replace('/', '\\'));
                }
                assetNamesFromMetadata = AssetMetadata.Map.Count;
                foreach (string file in AssetMetadata.Map.Keys) {
                    if (assets.Contains(file)) {
                        continue;
                    }
                    assets.Add(file);
                }
                assetNamesFromCache = cachedAssets.Count;
                foreach (string file in cachedAssets.Keys) {
                    if (assets.Contains(file)) {
                        continue;
                    }
                    assets.Add(file);
                }
                assetNames = assets;
            }
            return assetNames;
        }
        
        private static List<string> TraverseThrough(string dir, List<string> list = null) {
            if (list == null) {
                list = new List<string>();
            }

            if (!Directory.Exists(dir)) {
                return list;
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

        [MonoModIgnore] private static Dictionary<string, byte[]> cachedAssets;
        public static Dictionary<string, byte[]> GetCachedAssets() {
            if (cachedAssets == null) {
                cachedAssets = new Dictionary<string, byte[]>();
            }
            return cachedAssets;
        }

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
            string[] assetNames_ = new string[count];
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
                if (!File.Exists(filePath)) {
                    Directory.GetParent(filePath).Create();
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
        protected override Stream OpenStream(string assetName) {
            if (FEZModEngine.DumpAllResources) {
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
            if (File.Exists(filePath)) {
                FileStream fis = new FileStream(filePath, FileMode.Open);
                return fis;
            }
            if (FEZModEngine.DumpResources) {
                Directory.GetParent(filePath).Create();
                ModLogger.Log("FEZMod.Engine", assetName + " -> " + filePath);
                Stream ois = OpenStream_(assetName);
                FileStream fos = new FileStream(filePath, FileMode.CreateNew);
                ois.CopyTo(fos);
                ois.Close();
                fos.Close();
            }
            return OpenStream_(assetName);
        }
        
        protected Stream OpenStream_(string assetName_) {
            string assetName = assetName_.ToLowerInvariant().Replace('/', '\\');
            
            byte[] data;
            if (AssetDataCache.Persistent.TryGetValue(assetName, out data)) {
                return new MemoryStream(data);
            }
            
            CachedAssetData cached;
            if (AssetDataCache.Temporary.TryGetValue(assetName, out cached)) {
                cached.References++;
                cached.Age = 0;
                return new MemoryStream(data);
            }
            
            if (AssetDataCache.Preloaded.TryGetValue(assetName, out data)) {
                AssetDataCache.Temporary[assetName] = new CachedAssetData() {
                    Data = data,
                    References = 1
                };
                return new MemoryStream(data);
            }
            
            AssetMetadata metadata;
            if (AssetMetadata.Map.TryGetValue(assetName, out metadata)) {
                Stream assetStream;
                if (metadata.Assembly == null) {
                    assetStream = File.OpenRead(metadata.File);
                } else {
                    assetStream = metadata.Assembly.GetManifestResourceStream(metadata.File);
                }
                if (metadata.Length == 0) {
                    return assetStream;
                }
                LimitedStream ls = new LimitedStream(assetStream, metadata.Offset, metadata.Length);
                if (FEZModEngine.Settings.DataCache == DataCacheMode.Smart) {
                    AssetDataCache.Temporary[assetName] = new CachedAssetData() {
                        Data = ls.ToArray(),
                        References = 1
                    };
                    return ls;
                }
                return ls;
            }

            return orig_OpenStream(assetName_);
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
            
            if (AssetMetadata.Map.ContainsKey(assetName.ToLowerInvariant().Replace('/', '\\'))) {
                return true;
            }

            return orig_AssetExists(assetName);
        }

        public extern void orig_LoadEssentials();
        public void LoadEssentials() {
            if (FEZModEngine.Settings.DataCache == DataCacheMode.Default) {
                Dictionary<string, byte[]> preCachedAssets = cachedAssets;
                cachedAssets = null;
                orig_LoadEssentials();
                if (preCachedAssets != null) {
                    foreach (KeyValuePair<string, byte[]> pair in preCachedAssets) {
                        cachedAssets[pair.Key] = pair.Value;
                    }
                }
            } else {
                cachedAssets = new Dictionary<string, byte[]>(0);
                if (FEZModEngine.Settings.DataCache == DataCacheMode.Smart) {
                    AssetDataCache.CachePersistent("Essentials.pak");
                    AssetDataCache.UpdatePersistent("Updates.pak");
                } else {
                    ScanPackMetadata("Essentials.pak");
                }
            }

            FEZModEngine.PassLoadEssentials();
        }

        public extern void orig_Preload();
        public void Preload() {
            if (FEZModEngine.Settings.DataCache == DataCacheMode.Default) {
                orig_Preload();
            } else {
                ScanPackMetadata("Updates.pak");
			    ScanPackMetadata("Other.pak");
            }
            
            FEZModEngine.PassPreload();
        }
        
        public void ScanPackMetadata(string name) {
            string filePath = Path.Combine(RootDirectory, name);
            if (!File.Exists(filePath)) {
                return;
            }
            using (FileStream packStream = File.OpenRead(filePath)) {
                using (BinaryReader packReader = new BinaryReader(packStream)) {
                    int count = packReader.ReadInt32();
                    for (int i = 0; i < count; i++) {
                        string file = packReader.ReadString();
                        int length = packReader.ReadInt32();
                        if (!AssetMetadata.Map.ContainsKey(file)) {
                            AssetMetadata.Map[file] = new AssetMetadata(filePath, packStream.Position, length);
                        }
                        packStream.Seek(length, SeekOrigin.Current);
                    }
                }
            }
        }

    }
}

