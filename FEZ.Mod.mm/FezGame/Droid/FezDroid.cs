using System;
using System.IO;
using FezGame.Mod;
using FezEngine.Mod;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Droid {
    public class FezDroid : FezModule {

        public override string Name { get { return "FEZDroid"; } }
        public override string Author { get { return "AngelDE98 & FEZMod contributors"; } }
        public override string Version { get { return FEZMod.Version; } }

        public static bool InAndroid = false;
        private static bool runningInAndroid {
            get {
                ModLogger.Log("FEZDroid", "Checking if running in Android");
                return Directory.Exists("/system/app") && Directory.Exists("/data") && Directory.Exists("/sdcard");
            }
        }

        public FezDroid() {
        }
        
        public override void PreInitialize() {
            if (runningInAndroid) {
                EngageFEZDroid();
            }
        }

        public override void ParseArgs(string[] args) {
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "--android") {
                    EngageFEZDroid();
                }
            }
        }
        
        public override void LoadComponents(Fez game) {
            if (InAndroid) {
                ServiceHelper.AddComponent(new FezDroidComponent(ServiceHelper.Game));
            }
        }
        
        public static void EngageFEZDroid() {
            if (InAndroid) {
                return;
            }
            ModLogger.Log("FEZDroid", "Android mode engaged!");
            InAndroid = true;
            
            #if !FNA
            //FIXME Use newest 1.12 binaries
            Fez.NoLighting = true;
            #endif
            Fez.NoSteamworks = true;
            if (FEZMod.DisableCloudSaves != null) {
                FEZMod.DisableCloudSaves.SetValue(null, false);
            }
            
            FEZModEngine.Settings.CacheDisabled = true;
            FEZModEngine.Settings.MusicCache = MusicCacheMode.Disabled;
        }

    }
}

