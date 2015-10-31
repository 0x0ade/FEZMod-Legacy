using System;
using System.Collections.Generic;
using System.Reflection;
using FezGame.Mod;
using FezGame;
using FezEngine.Tools;
using FezEngine.Services;
using FezGame.Services;
using FezGame.Components;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using FezEngine.Structure;
using EasyStorage;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using FezEngine.Structure.Scripting;
using MonoMod.JIT;

namespace FezGame.Mod {
    public static class FEZMod {
        //FEZMod metadata
        public static string Version = "0.3a8";
        public static Version MODVersion = new Version(Version.IndexOf('a') == -1 ? Version : Version.Substring(0, Version.IndexOf('a')));
        public static Version FEZVersion;

        //FEZ version-dependent reflection
        public static FieldInfo DisableCloudSaves;

        //Modules and configuration
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
        public static bool GetComponentsAsServices = false;
        public static bool HandleComponents = false;
        public static bool CreatingThumbnail = false;
        private static string loadingLevel = null;
        public static string LoadingLevel {
            get {
                return loadingLevel;
            }
            set {
                if (value != null) {
                    ServiceHelper.Get<IGameStateManager>().Loading = true;
                }
                loadingLevel = value;
            }
        }
        public static double GameSpeed = 1d;
        #if FNA
        public static TimeSpan? ForceTimestep = null;
        public static bool Smooth = false;
        #endif
        public static GameTime UpdateGameTime;
        public static GameTime DrawGameTime;
        public static bool OverrideCultureManuallyBecauseMonoIsA_____ = false;

        //Other configuration
        public static bool LoadedEssentials { get; private set; }
        public static bool Preloaded { get; private set; }

        public static bool InAndroid = false;
        private static bool runningInAndroid {
            get {
                ModLogger.Log("FEZDroid", "Checking if running in Android");
                return Directory.Exists("/system/app");
            }
        }

        public static List<Assembly> LoadedAssemblies = new List<Assembly>();

        public static void PreInitialize(string[] args) {
            PreInitialize();
            ParseArgs(args);
        }

        private static int TestMonoModJIT(string test) {
            Console.WriteLine("Calling something unpatched.");
            Console.WriteLine("Assembly: " + Assembly.GetExecutingAssembly().FullName);
            Console.WriteLine("Passed arg: " + test);

            try {
                MonoModJITHandler.MMRun(null, test);
            } catch (MonoModJITPseudoException e) {
                return (int) e.Value;
            }

            Console.WriteLine("Calling something only when patched.");

            return -42;
        }

        public static void PreInitialize() {
            ModLogger.Clear();
            ModLogger.Log("FEZMod", "JustAnotherFEZMod (FEZMod) "+FEZMod.Version);

            try {
                FEZVersion = new Version(Fez.Version.IndexOf('a') == -1 ? Fez.Version : Fez.Version.Substring(0, Fez.Version.IndexOf('a')));
            } catch (Exception e) {
                ModLogger.Log("FEZMod", "Unknown FEZ version: " + Fez.Version);
                ModLogger.Log("FEZMod", "Exception: " + e);
                #if !FNA
                FEZVersion = new Version(1, 11); //Last non-FNA
                #else
                FEZVersion = new Version(1, 12); //First FNA
                #endif
            }
            Fez.Version = Fez.Version + " | " + FEZMod.Version;

            //Console.WriteLine("JIT test return: " + TestMonoModJIT("Hello, World!"));

            DisableCloudSaves = typeof(PCSaveDevice).GetField("DisableCloudSaves");

            Fez.NoSteamworks = true;
            if (DisableCloudSaves != null) {
                DisableCloudSaves.SetValue(null, true);
            }

            Type typeCultureInfo = typeof(CultureInfo);
            PropertyInfo propDefaultThreadCurrentCulture = typeCultureInfo.GetProperty("DefaultThreadCurrentCulture", BindingFlags.Public | BindingFlags.Static);
            if (propDefaultThreadCurrentCulture != null) {
                //.NET 4.5+ supports it natively without any reflection hacks, but as the build target is .NET 4.0, this is required.
                ModLogger.Log("FEZMod", "Running .NET 4.5+ codepath for setting the default culture.");
                propDefaultThreadCurrentCulture.SetValue(null, CultureInfo.InvariantCulture, null);
            } else {
                //.NET 4.0 and older have got private static fields for this.
                FieldInfo fieldUserDefaultCulture_4_0;
                FieldInfo fieldUserDefaultCulture_2_0;
                if ((fieldUserDefaultCulture_4_0 = typeCultureInfo.GetField("s_userDefaultCulture", BindingFlags.NonPublic | BindingFlags.Static)) != null) {
                    //Our build target.
                    ModLogger.Log("FEZMod", "Running .NET 4.0 codepath for setting the default culture.");
                    fieldUserDefaultCulture_4_0.SetValue(null, CultureInfo.InvariantCulture);
                } else if ((fieldUserDefaultCulture_2_0 = typeCultureInfo.GetField("s_userDefaultCulture", BindingFlags.NonPublic | BindingFlags.Static)) != null) {
                    //uhh...
                    ModLogger.Log("FEZMod", "Running .NET 2.0 codepath for setting the default culture.");
                    fieldUserDefaultCulture_2_0.SetValue(null, CultureInfo.InvariantCulture);
                } else {
                    //Holy realm of Mono.
                    //The .NET reference source contains s_userDefaultCulture, so let's hope Mono will do so, too.
                    ModLogger.Log("FEZMod", "WARNING: DefaultThreadCurrentCulture could not be set. Falling back to alternatives can cause errors!");
                    ModLogger.Log("FEZMod", "This message will always appear to remind the authors of FEZMod to find another workaround.");
                    ModLogger.Log("FEZMod", "Also, Mono sucks at .NET accuracy.");

                    OverrideCultureManuallyBecauseMonoIsA_____ = true;
                    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                }
            }

            PreInitializeModules();
        }

        public static void PreInitializeModules() {
            ModLogger.Log("FEZMod", "Initializing FEZ mods...");
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
                if (!reference.Name.EndsWith(".mm")) {
                    continue;
                }
                PreInitializeModules(Assembly.Load(reference));
            }
            if (!assembly.GetName().Name.EndsWith(".mm")) {
                return;
            }
            ModLogger.Log("FEZMod", "Found referenced assembly "+assembly.GetName().Name);
            foreach (Type type in assembly.GetTypes()) {
                if (typeof(FezModule).IsAssignableFrom(type) && !type.IsAbstract) {
                    PreInitializeModule(type);
                }
            }
        }

        public static void PreInitializeModule(Type type) {
            FezModule module = (FezModule) type.GetConstructor(new Type[0]).Invoke(new object[0]);
            ModLogger.Log("FEZMod", "Pre-Initializing "+module.Name);
            module.PreInitialize();
            Modules.Add(module);
        }

        public static void ParseArgs(string[] args) {
            ModLogger.Log("FEZMod", "Checking for custom arguments...");

            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-sw" || args[i] == "--steamworks") {
                    ModLogger.Log("FEZMod", "Found -sw / --steamworks");
                    Fez.NoSteamworks = false;
                    if (DisableCloudSaves != null) {
                        DisableCloudSaves.SetValue(null, false);
                    }
                }
                if ((args[i] == "-l" || args[i] == "--load-level") && i+1 < args.Length) {
                    ModLogger.Log("FEZMod", "Found -l / --load-level: "+args[i+1]);
                    Fez.ForcedLevelName = args[i+1];
                    //Fez.SkipLogos = true;
                    Fez.SkipIntro = true;
                }
                if (args[i] == "-lc" || args[i] == "--level-chooser") {
                    ModLogger.Log("FEZMod", "Found -lc / --level-chooser");
                    Fez.LevelChooser = true;
                    //Fez.SkipLogos = true;
                    Fez.SkipIntro = true;
                }
                if (args[i] == "-ls" || args[i] == "--long-screenshot") {
                    if (i+1 < args.Length && args[i+1] == "double") {
                        ModLogger.Log("FEZMod", "Found -ls / --long-screenshot double");
                        Fez.DoubleRotations = true;
                    } else {
                        ModLogger.Log("FEZMod", "Found -ls / --long-screenshot");
                    }
                    Fez.LongScreenshot = true;
                }
                if (args[i] == "-pp" || args[i] == "--pixel-perfect") {
                    ModLogger.Log("FEZMod", "Found -pp / --pixel-perfect");
                    OverridePixelsPerTrixel = 1f;
                }
                if (args[i] == "-nohd" || args[i] == "--no-high-definition") {
                    ModLogger.Log("FEZMod", "Found -nohd / --no-high-definition");
                    EnableHD = false;
                }
                if (args[i] == "-pphd" || args[i] == "--pixel-perfect-high-definition") {
                    ModLogger.Log("FEZMod", "Found -pphd / --pixel-perfect-high-definition");
                    EnablePPHD = true;
                }
                if (args[i] == "-4k" || args[i] == "--ultra-high-definition") {
                    ModLogger.Log("FEZMod", "Found -4k / --ultra-high-definition");
                    CustomResolutions.Add(new int[]{3840, 2160});
                }
                if (args[i] == "-8k" || args[i] == "--ultra-ultra-high-definition") {
                    ModLogger.Log("FEZMod", "Found -8k / --ultra-ultra-high-definition");
                    CustomResolutions.Add(new int[]{4096, 2304});
                }
                if ((args[i] == "-cr" || args[i] == "--custom-resolution") && i+2 < args.Length) {
                    ModLogger.Log("FEZMod", "Found -cr / --custom-resolution");
                    CustomResolutions.Add(new int[]{int.Parse(args[i+1]), int.Parse(args[i+2])});
                }
                if (args[i] == "-dc" || args[i] == "--debug-controls") {
                    ModLogger.Log("FEZMod", "Found -dc / --debug-controls");
                    EnableDebugControls = true;
                }
                //TODO extract multiplayer from core
                if (args[i] == "-mp" || args[i] == "--multiplayer") {
                    ModLogger.Log("FEZMod", "Found -mp / --multiplayer");
                    EnableMultiplayer = true;
                    if (i+1 < args.Length && !args[i+1].StartsWith("-")) {
                        ModLogger.Log("FEZMod", "Connecting to "+args[i+1]);
                        NetworkGomezClient.Instance = new NetworkGomezClient(args[i+1]);
                    } else {
                        ModLogger.Log("FEZMod", "Hosting...");
                        NetworkGomezServer.Instance = new NetworkGomezServer();
                    }
                }
                if (args[i] == "-mpl" || args[i] == "--multiplayer-localhost") {
                    ModLogger.Log("FEZMod", "Found -mpl / --multiplayer-localhost");
                    EnableMultiplayerLocalhost = true;
                }
                //TODO extract FEZDroid from core
                if (args[i] == "--android") {
                    InAndroid = true;
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
                    ModLogger.Log("FEZMod.Engine", "Found -d / --dump");
                    MemoryContentManager.AssetExists("FEZMOD_WORKAROUND_DUMP");
                }
                if (args[i] == "-da" || args[i] == "--dump-all") {
                    ModLogger.Log("FEZMod.Engine", "Found -da / --dump-all");
                    MemoryContentManager.AssetExists("FEZMOD_WORKAROUND_DUMPALL");
                }
                if (args[i] == "-nf" || args[i] == "--no-flat") {
                    ModLogger.Log("FEZMod.Engine", "Found -nf / --no-flat");
                    MemoryContentManager.AssetExists("FEZMOD_WORKAROUND_NOFLAT");
                }
                if (args[i] == "-nc" || args[i] == "--no-cache") {
                    ModLogger.Log("FEZMod.Engine", "Found -nc / --no-cache");
                    MemoryContentManager.AssetExists("FEZMOD_WORKAROUND_NOCACHE");
                }
                if (args[i] == "-cme" || args[i] == "--custom-music-extract") {
                    ModLogger.Log("FEZMod.Engine", "Found -cme / --custom-music-extract");
                    MemoryContentManager.AssetExists("FEZMOD_WORKAROUND_CUSTOMMUSICEXTRACT");
                }
                if (args[i] == "-nme" || args[i] == "--no-music-extract") {
                    ModLogger.Log("FEZMod.Engine", "Found -nme / --no-music-extract");
                    MemoryContentManager.AssetExists("FEZMOD_WORKAROUND_NOMUSICEXTRACT");
                }
                //Version-dependant options
                #if FNA
                //Hurtz options missing in FEZ 1.12 devbuilds (or I can't find them)
                if (args[i] == "-60hz" || args[i] == "--force-60hz") {
                    ModLogger.Log("FEZMod", "Found -60hz / --force-60hz");
                    ForceTimestep = TimeSpan.FromSeconds(1d / 60d);
                }
                if (args[i] == "-120hz" || args[i] == "--force-120hz") {
                    ModLogger.Log("FEZMod", "Found -120hz / --force-120hz");
                    ForceTimestep = TimeSpan.FromSeconds(1d / 120d);
                }
                //The FEZ 1.12 devbuilds seem to forcibly tick at 60hz
                if (args[i] == "-s" || args[i] == "--smooth") {
                    ModLogger.Log("FEZMod", "Found -s / --smooth");
                    Smooth = true;
                }
                #endif
            }

            if (InAndroid || runningInAndroid) {
                ModLogger.Log("FEZDroid", "Android mode engaged!");
                InAndroid = true;
                Fez.SkipIntro = true;
                Fez.SkipLogos = true;
                MemoryContentManager.AssetExists("FEZMOD_WORKAROUND_NOCACHE");
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

                ConstructorInfo dmConst = typeof(DisplayMode).GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, 
                #if FNA
                    new Type[] {typeof(int), typeof(int), typeof(SurfaceFormat)}, null);
                allModes.Add((DisplayMode) dmConst.Invoke(new object[] {1280, 720, SurfaceFormat.Color}));
                allModes.Add((DisplayMode) dmConst.Invoke(new object[] {1920, 1080, SurfaceFormat.Color}));
                #else
                    new Type[] {typeof(int), typeof(int), typeof(int), typeof(SurfaceFormat)}, null);
                allModes.Add((DisplayMode) dmConst.Invoke(new object[] {1280, 720, 60, SurfaceFormat.Color}));
                allModes.Add((DisplayMode) dmConst.Invoke(new object[] {1920, 1080, 60, SurfaceFormat.Color}));
                #endif
                foreach (int[] resolution in CustomResolutions) {
                    #if FNA
                    allModes.Add((DisplayMode) dmConst.Invoke(new object[] {resolution[0], resolution[1], SurfaceFormat.Color}));
                    #else
                    allModes.Add((DisplayMode) dmConst.Invoke(new object[] {resolution[0], resolution[1], 60, SurfaceFormat.Color}));
                    #endif
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
                    #if !FNA
                    if (mode.RefreshRate == 60) {
                    #endif
                        SettingsManager.Resolutions.Add(mode);
                    #if !FNA
                    } else {
                        SettingsManager.Resolutions.Add((DisplayMode) dmConst.Invoke(new object[] {mode.Width, mode.Height, 60, SurfaceFormat.Color}));
                    }
                    #endif
                }

                SettingsManager.Resolutions.Sort(new Comparison<DisplayMode>((x, y) => x.Width * x.Height - y.Width * y.Height));
            }
            
            ServiceHelper.Game.Exiting += (sender, e) => Exit();

            CallInEachModule("Initialize", new object[0]);
        }

        //Hooked FEZ methods or calls in each module
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
                    NetworkGomezServer.Instance.Broadcast();
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

        public static string ProcessLevelName(string levelName) {
            return CallInEachModule<string>("ProcessLevelName", levelName);
        }

        public static void ProcessLevelData(Level levelData) {
            CallInEachModule("ProcessLevelData", new object[] {levelData});
        }

        public static void HandleCrash(Exception e) {
            CallInEachModule("HandleCrash", new object[] {e});
        }

        //Additional methods
        //...

        //Helper methods
        private static void CallInEachModule(string methodName, object[] args) {
            Type[] argsTypes = Type.GetTypeArray(args);
            foreach (FezModule module in Modules) {
                module.GetType().GetMethod(methodName, argsTypes).Invoke(module, args);
            }
        }

        private static T CallInEachModule<T>(string methodName, T arg) {
            Type[] argsTypes = { typeof(T) };
            object[] args = { arg };
            foreach (FezModule module in Modules) {
                arg = (T) module.GetType().GetMethod(methodName, argsTypes).Invoke(module, args);
            }
            return arg;
        }

    }
}

