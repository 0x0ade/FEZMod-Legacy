using System;
using System.Collections.Generic;
using System.IO;
using FezEngine.Structure;
using FezEngine.Services;
using FezEngine.Mod;
using Microsoft.Xna.Framework.Content;
using MonoMod;
using System.Reflection;
using FezEngine.Tools;

namespace FezEngine.Mod {    
    public static class AssetDataCache {
        
        public static Dictionary<string, byte[]> Persistent = new Dictionary<string, byte[]>();
        
        public static Dictionary<string, CachedAssetData> Temporary = new Dictionary<string, CachedAssetData>();
        
        public static Dictionary<string, byte[]> Preloaded = new Dictionary<string, byte[]>();
        
        public static void CachePersistent(string name) {
            string filePath = Path.Combine(ServiceHelper.Game.Content.RootDirectory, name);
            if (!File.Exists(filePath)) {
                return;
            }
            using (FileStream packStream = File.OpenRead(filePath)) {
                using (BinaryReader packReader = new BinaryReader(packStream)) {
                    int count = packReader.ReadInt32();
                    for (int i = 0; i < count; i++) {
                        string file = packReader.ReadString();
                        int length = packReader.ReadInt32();
                        if (!Persistent.ContainsKey(file)) {
                            Persistent[file] = packReader.ReadBytes(length);
                        } else {
                            packStream.Seek(length, SeekOrigin.Current);
                        }
                    }
                }
            }
        }
        
        public static void UpdatePersistent(string name) {
            string filePath = Path.Combine(ServiceHelper.Game.Content.RootDirectory, name);
            if (!File.Exists(filePath)) {
                return;
            }
            using (FileStream packStream = File.OpenRead(filePath)) {
                using (BinaryReader packReader = new BinaryReader(packStream)) {
                    int count = packReader.ReadInt32();
                    for (int i = 0; i < count; i++) {
                        string file = packReader.ReadString();
                        int length = packReader.ReadInt32();
                        if (Persistent.ContainsKey(file)) {
                            Persistent[file] = packReader.ReadBytes(length);
                        } else {
                            packStream.Seek(length, SeekOrigin.Current);
                        }
                    }
                }
            }
        }
    }
    
    public class CachedAssetData {
        public byte[] Data;
        public int References;
        public int Age;
    }
}