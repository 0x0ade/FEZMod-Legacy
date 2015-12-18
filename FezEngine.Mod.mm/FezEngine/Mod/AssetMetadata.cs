using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ContentSerialization.Attributes;

namespace FezEngine.Mod {
    public class AssetMetadata {
        
        public static Dictionary<string, AssetMetadata> Map = new Dictionary<string, AssetMetadata>();
        
        public string File;
        [Serialization(Ignore = true)]
        public Assembly Assembly;
        public string AssemblyName;
        public long Offset;
        public int Length;
        
        public AssetMetadata() {
        }
        
        public AssetMetadata(string file, long offset, int length)
            : this() {
            File = file;
            Offset = offset;
            Length = length;
        }
        
        public AssetMetadata(Assembly assembly, string file, long offset, int length)
            : this(file, offset, length) {
            Assembly = assembly;
            AssemblyName = assembly.GetName().Name;
        }
        
    }
}