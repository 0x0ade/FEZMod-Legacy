using System;
using System.Collections.Generic;
using System.Reflection;
using Common;
using FezGame;
using FezEngine.Tools;
using FezEngine.Services;
using FezGame.Services;
using FezGame.Components;
using FezGame.Structure;

namespace FezGame.Mod {
    public static class FEZMod {
        public static string Version = "0.1.2";

        public static List<FezModule> Modules = new List<FezModule>();

        public static bool IsAlwaysTurnable = false;
        public static float OverridePixelsPerTrixel = 0f;
        public static bool EnableDebugControls = false;
        public static bool EnableQuickWarp = true;
        public static bool EnableFEZometric = true;

        public static bool LoadedEssentials { get; private set; }
        public static bool Preloaded { get; private set; }

        private static List<Assembly> LoadedAssemblies = new List<Assembly>();

        public static void PreInitialize(string[] args) {
            PreInitialize();
            ParseArgs(args);
        }

        public static void PreInitialize() {
            ModLogger.Clear();
            ModLogger.Log("JAFM", "JustAnotherFEZMod (JAFM) "+FEZMod.Version);

            Fez.Version = "FEZ: "+ Fez.Version + " | JAFM: " + FEZMod.Version;

            Fez.NoSteamworks = true;

            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            PreInitializeModules();
        }

        public static void PreInitializeModules() {
            ModLogger.Log("JAFM", "Initializing FEZ mods...");
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                PreInitializeModules(assembly);
            }
        }

        public static void PreInitializeModules(Assembly assembly) {
            if (LoadedAssemblies.Contains(assembly)) {
                return;
            }
            LoadedAssemblies.Add(assembly);
            foreach (AssemblyName reference in assembly.GetReferencedAssemblies()) {
                PreInitializeModules(Assembly.Load(reference));
            }
            ModLogger.Log("JAFM", "Found referenced assembly "+assembly.GetName().Name);
            if (!assembly.GetName().Name.EndsWith(".mm")) {
                return;
            }
            foreach (Type type in assembly.GetTypes()) {
                if (typeof(FezModule).IsAssignableFrom(type) && !type.IsAbstract) {
                    PreInitializeModule(type);
                }
            }
        }

        public static void PreInitializeModule(Type type) {
            FezModule module = (FezModule) type.GetConstructor(new Type[0]).Invoke(new object[0]);
            ModLogger.Log("JAFM", "Pre-Initializing "+module.Name);
            module.PreInitialize();
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
                    if (i+1 < args.Length && args[i+1] == "double") {
                        ModLogger.Log("JAFM", "Found -ls / --long-screenshot double");
                        Fez.DoubleRotations = true;
                    } else {
                        ModLogger.Log("JAFM", "Found -ls / --long-screenshot");
                    }
                    Fez.LongScreenshot = true;
                }
                if (args[i] == "-pp" || args[i] == "--pixel-perfect") {
                    ModLogger.Log("JAFM", "Found -pp / --pixel-perfect");
                    OverridePixelsPerTrixel = 1f;
                }
                if (args[i] == "-dc" || args[i] == "--debug-controls") {
                    ModLogger.Log("JAFM", "Found -dc / --debug-controls");
                    EnableDebugControls = true;
                }
                /*
                Explaination as of why the workaround still is required:
                A module type won't be copied to the patched DLL, which means the
                references to MCM will not be touched at all. These references
                will point to the patch DLL, not the patchED DLL. AssetExists on the
                other hand is inside the patchED DLL and thus has got the correct
                references.
                */
                if (args[i] == "-d" || args[i] == "--dump") {
                    ModLogger.Log("JAFM.Engine", "Found -d / --dump");
                    MemoryContentManager.AssetExists("JAFM_WORKAROUND_DUMP");
                }
                if (args[i] == "-da" || args[i] == "--dump-all") {
                    ModLogger.Log("JAFM.Engine", "Found -da / --dump-all");
                    MemoryContentManager.AssetExists("JAFM_WORKAROUND_DUMPALL");
                }
                if (args[i] == "-nf" || args[i] == "--no-flat") {
                    ModLogger.Log("JAFM.Engine", "Found -nf / --no-flat");
                    MemoryContentManager.AssetExists("JAFM_WORKAROUND_NOFLAT");
                }
                if (args[i] == "-nc" || args[i] == "--no-cache") {
                    ModLogger.Log("JAFM.Engine", "Found -nc / --no-cache");
                    MemoryContentManager.AssetExists("JAFM_WORKAROUND_NOCACHE");
                }
            }

            CallInEachModule("ParseArgs", new object[] {args});
        }

        public static void Initialize() {
            ServiceHelper.AddComponent(new FEZModComponent(ServiceHelper.Game));

            if (EnableDebugControls) {
                ServiceHelper.AddComponent(new DebugControls(ServiceHelper.Game));
            }

            CallInEachModule("Initialize", new object[0]);
        }

        public static void Exit() {
            CallInEachModule("Exit", new object[0]);
        }

        public static void LoadEssentials() {
            CallInEachModule("LoadEssentials", new object[0]);
            LoadedEssentials = true;
        }

        public static void Preload() {
            CallInEachModule("Preload", new object[0]);
            Preloaded = true;
        }

        public static void SaveClear(SaveData saveData) {
            CallInEachModule("SaveClear", new object[] {saveData});
        }

        public static void SaveClone(SaveData source, SaveData dest) {
            CallInEachModule("SaveClone", new object[] {source, dest});
        }

        public static void SaveRead(SaveData saveData, CrcReader reader) {
            CallInEachModule("SaveRead", new object[] {saveData, reader});
        }

        public static void SaveWrite(SaveData saveData, CrcWriter writer) {
            CallInEachModule("SaveWrite", new object[] {saveData, writer});
        }

        private static void CallInEachModule(String methodName, object[] args) {
            Type[] argsTypes = Type.GetTypeArray(args);
            foreach (FezModule module in Modules) {
                module.GetType().GetMethod(methodName, argsTypes).Invoke(module, args);
            }
        }

    }
}

