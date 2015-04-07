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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Mod {
    public static class FEZMod {
        public static string Version = "0.2";

        public static List<FezModule> Modules = new List<FezModule>();

        public static bool IsAlwaysTurnable = false;
        public static float OverridePixelsPerTrixel = 0f;
        public static bool EnableDebugControls = false;
        public static bool EnableQuickWarp = true;
        public static bool EnableFEZometric = true;
        public static bool EnableBugfixes = true;
        public static bool EnableHD = true;
        public static bool EnablePPHD = false;
        public static List<int[]> CustomResolutions = new List<int[]>();
        public static bool EnableMultiplayer = false;
        public static bool EnableMultiplayerLocalhost = false;

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

            Fez.Version = Fez.Version + " | " + FEZMod.Version;

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
                if (args[i] == "-nohd" || args[i] == "--no-high-definition") {
                    ModLogger.Log("JAFM", "Found -nohd / --no-high-definition");
                    EnableHD = false;
                }
                if (args[i] == "-pphd" || args[i] == "--pixel-perfect-high-definition") {
                    ModLogger.Log("JAFM", "Found -pphd / --pixel-perfect-high-definition");
                    EnablePPHD = true;
                }
                if (args[i] == "-4k" || args[i] == "--ultra-high-definition") {
                    ModLogger.Log("JAFM", "Found -4k / --ultra-high-definition");
                    CustomResolutions.Add(new int[]{3840, 2160});
                }
                if (args[i] == "-8k" || args[i] == "--ultra-ultra-high-definition") {
                    ModLogger.Log("JAFM", "Found -8k / --ultra-ultra-high-definition");
                    CustomResolutions.Add(new int[]{4096, 2304});
                }
                if ((args[i] == "-cr" || args[i] == "--custom-resolution") && i+2 < args.Length) {
                    ModLogger.Log("JAFM", "Found -cr / --custom-resolution");
                    CustomResolutions.Add(new int[]{int.Parse(args[i+1]), int.Parse(args[i+2])});
                }
                if (args[i] == "-dc" || args[i] == "--debug-controls") {
                    ModLogger.Log("JAFM", "Found -dc / --debug-controls");
                    EnableDebugControls = true;
                }
                if (args[i] == "-mp" || args[i] == "--multiplayer") {
                    ModLogger.Log("JAFM", "Found -mp / --multiplayer");
                    EnableMultiplayer = true;
                    if (i+1 < args.Length && !args[i+1].StartsWith("-")) {
                        ModLogger.Log("JAFM", "Connecting to "+args[i+1]);
                        NetworkGomezClient.Instance = new NetworkGomezClient(args[i+1]);
                    } else {
                        ModLogger.Log("JAFM", "Hosting...");
                        NetworkGomezServer.Instance = new NetworkGomezServer();
                    }
                }
                if (args[i] == "-mpl" || args[i] == "--multiplayer-localhost") {
                    ModLogger.Log("JAFM", "Found -mpl / --multiplayer-localhost");
                    EnableMultiplayerLocalhost = true;
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
                if (args[i] == "-nme" || args[i] == "--no-music-extract") {
                    ModLogger.Log("JAFM.Engine", "Found -nme / --no-music-extract");
                    MemoryContentManager.AssetExists("JAFM_WORKAROUND_NOMUSICEXTRACT");
                }
            }

            CallInEachModule("ParseArgs", new object[] {args});
        }

        public static void Initialize() {
            if (EnableHD) {
                //TODO clean up garbage. Even if it's called just once, is optimizable.
                SettingsManager.Resolutions.Clear();
                DisplayModeCollection supportedModes = GraphicsAdapter.DefaultAdapter.SupportedDisplayModes;
                List<DisplayMode> allModes = new List<DisplayMode>();
                allModes.AddRange(supportedModes);

                ConstructorInfo dmConst = typeof(DisplayMode).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, 
                    new Type[] {typeof(int), typeof(int), typeof(int), typeof(SurfaceFormat)}, null);
                allModes.Add((DisplayMode) dmConst.Invoke(new object[] {1280, 720, 60, SurfaceFormat.Color}));
                allModes.Add((DisplayMode) dmConst.Invoke(new object[] {1920, 1080, 60, SurfaceFormat.Color}));
                foreach (int[] resolution in CustomResolutions) {
                    allModes.Add((DisplayMode) dmConst.Invoke(new object[] {resolution[0], resolution[1], 60, SurfaceFormat.Color}));
                }

                foreach (DisplayMode mode in allModes) {
                    bool added = false;
                    foreach (DisplayMode mode_ in SettingsManager.Resolutions) {
                        if (mode.Width == mode_.Width && mode.Height == mode_.Height) {
                            added = true;
                            break;
                        }
                    }
                    if (added) {
                        continue;
                    }
                    if (mode.RefreshRate == 60) {
                        SettingsManager.Resolutions.Add(mode);
                    } else {
                        SettingsManager.Resolutions.Add((DisplayMode) dmConst.Invoke(new object[] {mode.Width, mode.Height, 60, SurfaceFormat.Color}));
                    }
                }

                SettingsManager.Resolutions.Sort(new Comparison<DisplayMode>((x, y) => x.Width * x.Height - y.Width * y.Height));
            }
            
            ServiceHelper.Game.Exiting += (sender, e) => Exit();

            CallInEachModule("Initialize", new object[0]);
        }

        public static void LoadComponents(Fez game) {
            ServiceHelper.AddComponent(new FEZModComponent(ServiceHelper.Game));

            if (EnableDebugControls) {
                ServiceHelper.AddComponent(new DebugControls(ServiceHelper.Game));
            }

            if (EnableMultiplayer) {
                ServiceHelper.AddComponent(new SlaveGomezHost(ServiceHelper.Game));
                if (NetworkGomezClient.Instance != null) {
                    NetworkGomezClient.Instance.Start();
                } else if (NetworkGomezServer.Instance != null) {
                    ServiceHelper.Get<ISoundManager>().InitializeLibrary();
                    NetworkGomezServer.Instance.StartListening();
                }
            }

            CallInEachModule("LoadComponents", new object[1] {game});
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

            if (EnableBugfixes) {
                saveData.HasFPView = saveData.HasFPView || saveData.HasStereo3D;
            }
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

