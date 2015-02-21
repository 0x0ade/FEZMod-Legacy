using System;
using System.Collections.Generic;
using System.Reflection;
using Common;
using FezGame;
using FezEngine.Tools;

namespace FezGame.Mod {
    public static class FEZMod {
        public static string Version = "0.0.5";

        public static List<FezModule> Modules = new List<FezModule>();

        public static bool IsAlwaysTurnable = false;

        public static void Initialize(string[] args) {
            Initialize();
            ParseArgs(args);
        }

        public static void Initialize() {
            ModLogger.Clear();
            ModLogger.Log("JAFM", "JustAnotherFEZMod (JAFM) "+FEZMod.Version);

            Fez.Version = FEZMod.Version;
            Fez.Version += " (JustAnotherFEZMod)";

            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            InitializeModules();
        }

        public static void InitializeModules() {
            ModLogger.Log("JAFM", "Initializing FEZ mods...");
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (Type type in assembly.GetTypes()) {
                    if (typeof(FezModule).IsAssignableFrom(type) && !type.IsAbstract) {
                        InitializeModule(type);
                    }
                }
            }
        }

        public static void InitializeModule(Type type) {
            FezModule module = (FezModule) type.GetConstructor(new Type[0]).Invoke(new object[0]);
            ModLogger.Log("JAFM", "Initializing "+module.Name);
            module.Initialize();
            Modules.Add(module);
        }

        public static void ParseArgs(string[] args) {
            ModLogger.Log("JAFM", "Checking for custom arguments...");

            for (int i = 0; i < args.Length; i++) {
                if ((args[i] == "-l" || args[i] == "--load-level") && i+1 < args.Length) {
                    ModLogger.Log("JAFM", "Found -l / --load-level: "+args[i+1]);
                    Fez.ForcedLevelName = args[i+1];
                    //Fez.SkipLogos = true;
                    Fez.SkipIntro = true;
                }
                if (args[i] == "-lc" || args[i] == "--level-chooser") {
                    ModLogger.Log("JAFM", "Found -lc / --level-chooser");
                    Fez.LevelChooser = true;
                    //Fez.SkipLogos = true;
                    Fez.SkipIntro = true;
                }
                if (args[i] == "-ls" || args[i] == "--long-screenshot") {
                    if (i+1 < args.Length && !args[i+1].StartsWith("-")) {
                        ModLogger.Log("JAFM", "Found -ls / --long-screenshot: "+args[i+1]);
                        Fez.ForcedLevelName = args[i+1];
                    } else {
                        ModLogger.Log("JAFM", "Found -ls / --long-screenshot");
                    }
                    Fez.LongScreenshot = true;
                    Fez.DoubleRotations = true;
                    //Fez.SkipLogos = true;
                    Fez.SkipIntro = true;
                }
                if (args[i] == "-d" || args[i] == "--dump") {
                    ModLogger.Log("JAFM", "Found -d / --dump");
                    MemoryContentManager.AssetExists("JAFM_DUMP_WORKAROUND");
                }
                if (args[i] == "-da" || args[i] == "--dump-all") {
                    ModLogger.Log("JAFM", "Found -da / --dump-all");
                    MemoryContentManager.AssetExists("JAFM_DUMPALL_WORKAROUND");
                }
                if (args[i] == "-nf" || args[i] == "--no-flat") {
                    ModLogger.Log("JAFM", "Found -nf / --no-flat");
                    MemoryContentManager.AssetExists("JAFM_NOFLAT_WORKAROUND");
                }
                if (args[i] == "-nc" || args[i] == "--no-cache") {
                    ModLogger.Log("JAFM", "Found -nc / --no-cache");
                    MemoryContentManager.AssetExists("JAFM_NOCACHE_WORKAROUND");
                }
            }

            CallInEachModule("ParseArgs", args);
        }

        private static void CallInEachModule(String methodName, object[] args) {
            foreach (FezModule module in Modules) {
                module.GetType().GetMethod(methodName).Invoke(module, args);
            }
        }

    }
}

