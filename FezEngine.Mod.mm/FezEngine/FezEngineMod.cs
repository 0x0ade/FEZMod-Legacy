using Common;
using System;
using FezGame.Mod;
using FezEngine.Tools;
using FezEngine.Structure;

namespace FezEngine {
    public class FezEngineMod : FezModule {
        public override string Name { get { return "JAFM.Engine"; } }
        public override string Author { get { return "AngelDE98 & JAFM contributors"; } }
        public override string Version { get { return FEZMod.Version; } }

        public FezEngineMod() {
        }

        public override void ParseArgs(string[] args) {
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-d" || args[i] == "--dump") {
                    ModLogger.Log("JAFM.Engine", "Found -d / --dump");
                    MemoryContentManager.DumpResources = true;
                }
                if (args[i] == "-da" || args[i] == "--dump-all") {
                    ModLogger.Log("JAFM.Engine", "Found -da / --dump-all");
                    MemoryContentManager.DumpAllResources = true;
                }
                if (args[i] == "-nf" || args[i] == "--no-flat") {
                    ModLogger.Log("JAFM.Engine", "Found -nf / --no-flat");
                    Level.FlatDisabled = true;
                }
                if (args[i] == "-nc" || args[i] == "--no-cache") {
                    ModLogger.Log("JAFM.Engine", "Found -nc / --no-cache");
                    MemoryContentManager.CacheDisabled = true;
                }
            }
        }

    }
}

