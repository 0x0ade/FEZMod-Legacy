using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace FezEngine.Tools {
    public class MemoryContentManager {

        private static Dictionary<String, byte[]> cachedAssets;

        public static bool DumpResources = false;
        public static bool DumpAllResources = false;
        private static int DumpAllResourcesCount = 0;

        public void DumpAll() {
            if (cachedAssets == null) {
                Console.WriteLine("RESOURCEMOD: Cached assets do not exist; ignoring...");
            }

            int dumped = 0;

            int count = cachedAssets.Count;
            if (count <= DumpAllResourcesCount) {
                return;
            }
            DumpAllResourcesCount = count;
            Console.WriteLine("RESOURCEMOD: Dumping "+count+" assets...");
            String[] assetNames = new String[count];
            cachedAssets.Keys.CopyTo(assetNames, 0);

            for (int i = 0; i < count; i++) {
                byte[] bytes = cachedAssets[assetNames[i]];
                string assetName = assetNames[i].ToLower();
                string filePath = ("Resources\\"+(assetName.ToLower())).Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString())+".xnb";
                FileInfo file = new FileInfo(filePath);
                if (!file.Exists) {
                    file.Directory.Create();
                    Console.WriteLine("RESOURCEMOD: "+(i+1)+" / "+count+": "+assetName+" -> "+filePath);
                    FileStream fos = new FileStream(filePath, FileMode.CreateNew);
                    fos.Write(bytes, 0, bytes.Length);
                    fos.Close();
                    dumped++;
                }
            }

            Console.WriteLine("RESOURCEMOD: Dumped: "+dumped+" / "+count);
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
                Console.WriteLine("RESOURCEMOD: "+assetName+" -> "+filePath);
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
            string filePath = ("Resources\\"+(assetName.ToLower())).Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString())+".xnb";
            FileInfo file = new FileInfo(filePath);
            if (file.Exists) {
                return true;
            }
            return orig_AssetExists(assetName);
        }

    }
}

