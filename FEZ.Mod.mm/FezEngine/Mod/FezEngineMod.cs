using System;
using System.Collections.Generic;
using FezGame.Mod;

namespace FezEngine.Mod {
    //This class belongs to FezEngine.Mod.mm, but is in FEZ.Mod.mm for accessibility.
    public class FezEngineMod : FezModule {

        public override string Name { get { return "FEZMod.Engine"; } }
        public override string Author { get { return "AngelDE98 & FEZMod contributors"; } }
        public override string Version { get { return FEZMod.Version; } }

        public static MusicCacheMode MusicCache = MusicCacheMode.Default;
        
        public static bool FlatDisabled = false;
        
        public static bool DumpResources = false;
        public static bool DumpAllResources = false;
        public static bool CacheDisabled = false;
        
        public static Dictionary<string, Tuple<string, long, int>> AssetMetadata = new Dictionary<string, Tuple<string, long, int>>();

        public FezEngineMod() {
        }

        public override void ParseArgs(string[] args) {
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-d" || args[i] == "--dump") {
                    ModLogger.Log("FEZMod.Engine", "Found -d / --dump");
                    DumpResources = true;
                }
                if (args[i] == "-da" || args[i] == "--dump-all") {
                    ModLogger.Log("FEZMod.Engine", "Found -da / --dump-all");
                    DumpAllResources = true;
                }
                if (args[i] == "-nf" || args[i] == "--no-flat") {
                    ModLogger.Log("FEZMod.Engine", "Found -nf / --no-flat");
                    FlatDisabled = true;
                }
                if (args[i] == "-nc" || args[i] == "--no-cache") {
                    ModLogger.Log("FEZMod.Engine", "Found -nc / --no-cache");
                    CacheDisabled = true;
                }
                if (args[i] == "-mc" || args[i] == "--music-cache") {
                    ModLogger.Log("FEZMod.Engine", "Found -mc / --music-cache");
                    MusicCache = MusicCacheMode.Enabled;
                }
                if (args[i] == "-mnc" || args[i] == "--music-no-cache") {
                    ModLogger.Log("FEZMod.Engine", "Found -mnc / --music-no-cache");
                    MusicCache = MusicCacheMode.Disabled;
                }
            }
        }

    }
}

