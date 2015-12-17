using System;
using System.Collections.Generic;
using System.Reflection;
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
using MonoMod.JIT;
using FezEngine.Mod;
using FezGame.Tools;
using FezEngine.Components;
#if FNA
using SDL2;
#endif

namespace FezGame.Mod {
    
    public abstract class FezModule : FezModuleCore {

        public FezModule() {
        }

        public virtual void InitializeMenu(MenuBase mb) {}
        public virtual void LoadComponents(Fez game) {}
        public virtual void SaveClear(SaveData saveData) {}
        public virtual void SaveClone(SaveData source, SaveData dest) {}
        public virtual void SaveRead(SaveData saveData, CrcReader reader) {}
        public virtual void SaveWrite(SaveData saveData, CrcWriter writer) {}
        
    }
    
    public static class FEZMod {
        //FEZMod metadata
        public static string Version = "0.3a8";
        public static Version MODVersion = new Version(Version.IndexOf('a') == -1 ? Version : Version.Substring(0, Version.IndexOf('a')));
        public static Version FEZVersion;

        //FEZ version-dependent reflection
        public static FieldInfo DisableCloudSaves;

        //Modules and configuration
        public static List<FezModuleCore> Modules = new List<FezModuleCore>();
        private static Type[] ModuleTypes;
        private static Dictionary<string, MethodInfo>[] ModuleMethods;
        
        public static bool IsAlwaysTurnable = false;
        public static float OverridePixelsPerTrixel = 0f;
        public static bool EnableDebugControls = false;
        public static bool EnableQuickWarp = true;
        public static bool EnableFEZometric = true;
        public static bool EnableBugfixes = true;
        public static bool EnableHD = true;
        public static List<int[]> CustomResolutions = new List<int[]>();
        public static bool EnableMultiplayer = false;
        public static bool EnableMultiplayerLocalhost = false;
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

        //Other configuration
        public static bool LoadedEssentials { get; private set; }
        public static bool Preloaded { get; private set; }
        public static MenuLevel Menu { get; private set; }
        private static float Menu_SinceRestartNoteShown = 0f;

        public static List<Assembly> LoadedAssemblies = new List<Assembly>();

        public static void PreInitialize(string[] args) {
            PreInitialize();
            ParseArgs(args);
        }

        private static int TestMonoModJIT(string test) {
            Console.WriteLine("Calling something unpatched.");
            Console.Write("Assembly: ");
            Console.WriteLine(Assembly.GetExecutingAssembly().FullName);
            Console.Write("Passed arg: ");
            Console.WriteLine(test);

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
            
            //Pre-initialize FEZModEngine link to FEZMod
            FEZModEngine.GetVersion = () => Version;
            FEZModEngine.SetVersion = (v) => Version = v;
            FEZModEngine.GetMODVersion = () => MODVersion;
            FEZModEngine.SetMODVersion = (v) => MODVersion = v;
            FEZModEngine.GetFEZVersion = () => FEZVersion;
            FEZModEngine.SetFEZVersion = (v) => FEZVersion = v;
            FEZModEngine.PassLoadEssentials = LoadEssentials;
            FEZModEngine.PassPreload = Preload;
            FEZModEngine.PassInitialize = Initialize;
            
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

            //Disable steamworks
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

                    FEZModEngine.OverrideCultureManuallyBecauseMonoIsA_____ = true;
                    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                }
            }
            
            //FEZMod custom texts
            TextPatchHelper.Static.Fallback["FEZModMenu"] = "FEZMOD MENU";
            TextPatchHelper.Static.Fallback["StreamAssetsDisk"] = "Stream data from: {0}";
            TextPatchHelper.Static.Fallback["StreamMusicType"] = "Stream music from: {0}";
            
            //Load settings and handle updates early enough for FEZMod itself
            FEZModEngine.Settings = FezModuleSettings.Load<FEZModSettings>("FEZMod.Settings.sdl", new FEZModSettings());
            
            if (new Version(FEZModEngine.Settings.LastVersion) < MODVersion) {
                ModLogger.Log("FEZMod", "Handling FEZMod update from " + FEZModEngine.Settings.LastVersion);
            }
            
            if (new Version(FEZModEngine.Settings.LastFEZVersion) < FEZVersion) {
                ModLogger.Log("FEZMod", "Handling FEZ update from " + FEZModEngine.Settings.LastFEZVersion);
            }
            
            FEZModEngine.Settings.LastVersion = MODVersion.ToString();
            FEZModEngine.Settings.LastFEZVersion = FEZVersion.ToString();
            
            FEZModEngine.Settings.Save();
            
            PreInitializeModules();
        }

        public static void PreInitializeModules() {
            ModLogger.Log("FEZMod", "Initializing FEZ mods...");
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                PreInitializeModules(assembly);
            }
            
            ModuleTypes = new Type[Modules.Count];
            ModuleMethods = new Dictionary<string, MethodInfo>[ModuleTypes.Length];
            for (int i = 0; i < ModuleTypes.Length; i++) {
                FezModuleCore module = Modules[i];
                ModuleTypes[i] = module.GetType();
                ModuleMethods[i] = new Dictionary<string, MethodInfo>();
            }
        }

        public static void PreInitializeModules(Assembly assembly) {
            if (LoadedAssemblies.Contains(assembly)) {
                return;
            }
            LoadedAssemblies.Add(assembly);
            foreach (AssemblyName reference in assembly.GetReferencedAssemblies()) {
                if (reference.Name.EndsWith(".mm")) {
                    //New MonoMod versions patch the .mms into the assemblies to mod
                    continue;
                }
                try {
                    PreInitializeModules(Assembly.Load(reference));
                } catch (Exception e) {
                    ModLogger.Log("FEZMod", "Error pre-initializing assembly " + reference + ":");
                    ModLogger.Log("FEZMod", e.ToString());
                }
            }
            if (assembly.GetName().Name.EndsWith(".mm")) {
                //New MonoMod versions patch the .mms into the assemblies to mod
                return;
            }
            ModLogger.Log("FEZMod", "Found referenced assembly "+assembly.GetName().Name);
            assembly.ScanAssemblyMetadataForContent();
            foreach (Type type in assembly.GetTypes()) {
                if (typeof(FezModuleCore).IsAssignableFrom(type) && !type.IsAbstract) {
                    PreInitializeModule(type);
                }
            }
        }

        public static void PreInitializeModule(Type type) {
            FezModuleCore module = (FezModuleCore) type.GetConstructor(Garbage.a_Type_0).Invoke(Garbage.a_object_0);
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
                    FEZModEngine.EnablePPHD = true;
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
                object[] args = {0, 0, SurfaceFormat.Color};
                #else
                    new Type[] {typeof(int), typeof(int), typeof(int), typeof(SurfaceFormat)}, null);
                object[] args = {0, 0, 60, SurfaceFormat.Color};
                #endif
                args[0] = 640; args[1] = 360; allModes.Add((DisplayMode) dmConst.Invoke(args));
                args[0] = 920; args[1] = 540; allModes.Add((DisplayMode) dmConst.Invoke(args));
                args[0] = 1280; args[1] = 720; allModes.Add((DisplayMode) dmConst.Invoke(args));
                args[0] = 1920; args[1] = 1080; allModes.Add((DisplayMode) dmConst.Invoke(args));
                foreach (int[] resolution in CustomResolutions) {
                    args[0] = resolution[0]; args[1] = resolution[1]; allModes.Add((DisplayMode) dmConst.Invoke(args));
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
                        args[0] = mode.Width; args[1] = mode.Height; SettingsManager.Resolutions.Add((DisplayMode) dmConst.Invoke(args));
                    }
                    #endif
                }

                SettingsManager.Resolutions.Sort(new Comparison<DisplayMode>((x, y) => x.Width * x.Height - y.Width * y.Height));
            }
            
            ServiceHelper.Game.Exiting += (sender, e) => Exit();

            CallInEachModule("Initialize", Garbage.a_object_0);
        }
        
        public static void InitializeMenu(MenuBase mb) {
            FEZModSettings tmpSettings = null;
            Action save = delegate() {
                if (tmpSettings != null) {
                    FEZModEngine.Settings = tmpSettings;
                    FEZModEngine.Settings.Save();
                }
                tmpSettings = FEZModEngine.Settings.ShallowClone();
            };
            save();
            
            MenuItem item;
            
            Menu_SinceRestartNoteShown = 0f;
            
            Menu = new MenuLevel() {
                Title = "FEZModMenu",
                AButtonString = "MenuApplyWithGlyph",
                BButtonString = "MenuSaveWithGlyph",
                IsDynamic = true,
                Oversized = true,
                Parent = mb.HelpOptionsMenu,
                OnReset = delegate() {
                },
                OnPostDraw = delegate(SpriteBatch batch, SpriteFont font, GlyphTextRenderer tr, float alpha) {
                    float scale = mb.Fonts.SmallFactor * batch.GraphicsDevice.GetViewScale();
                    float y = (float) batch.GraphicsDevice.Viewport.Height / 2f - (float) batch.GraphicsDevice.Viewport.Height / 3.825f;
                    if ((
                        Menu.SelectedIndex == 0 ||
                        Menu.SelectedIndex == 1
                    ) && mb.selectorPhase == SelectorPhase.Select) {
                        Menu_SinceRestartNoteShown = Math.Min(Menu_SinceRestartNoteShown + 0.05f, 1f);
                        tr.DrawCenteredString(batch, mb.Fonts.Small, StaticText.GetString("RequiresRestart"), new Color(1f, 1f, 1f, alpha * Menu_SinceRestartNoteShown), new Vector2(0f, y), scale);
                    } else {
                        Menu_SinceRestartNoteShown = Math.Max(Menu_SinceRestartNoteShown - 0.1f, 0f);
                    }
                }
            };
            
            item = Menu.AddItem<string>("StreamAssetsDisk", save, false,
                delegate() {
                    switch (tmpSettings.DataCache) {
                        case DataCacheMode.Default:
                            return "RAM";
                        case DataCacheMode.Disabled:
                            return "CARTRIDGE";
                        case DataCacheMode.Smart:
                            return "MAGIC";
                    }
                    return "UNKNOWN";
                },
                delegate(string lastValue, int change) {
                    int val = (int) tmpSettings.DataCache + change;
                    if (val < (int) DataCacheMode.Default) {
                        val = (int) DataCacheMode.Default;
                    }
                    if (val > (int) DataCacheMode.Disabled) {
                        val = (int) DataCacheMode.Disabled;
                    }
                    tmpSettings.DataCache = (DataCacheMode) val;
                }
            );
            item.UpperCase = true;
            
            item = Menu.AddItem<string>("StreamMusicType", save, false,
                delegate() {
                    switch (tmpSettings.MusicCache) {
                        case MusicCacheMode.Default:
                            return "DEFAULT MEDIUM";
                        case MusicCacheMode.Disabled:
                            return "CARTRIDGE";
                        case MusicCacheMode.Enabled:
                            return "RAM";
                    }
                    return "UNKNOWN";
                },
                delegate(string lastValue, int change) {
                    int val = (int) tmpSettings.MusicCache + change;
                    if (val < (int) MusicCacheMode.Default) {
                        val = (int) MusicCacheMode.Default;
                    }
                    if (val > (int) MusicCacheMode.Enabled) {
                        val = (int) MusicCacheMode.Enabled;
                    }
                    tmpSettings.MusicCache = (MusicCacheMode) val;
                }
            );
            item.UpperCase = true;
            
            mb.HelpOptionsMenu.AddItem("FEZModMenu", delegate() {
                mb.ChangeMenuLevel(Menu);
            });
            mb.MenuLevels.Add(Menu);
            
            CallInEachModule("InitializeMenu", Garbage.GetObjectArray(mb));
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

            CallInEachModule("LoadComponents", Garbage.GetObjectArray(game));
        }

        public static void Exit() {
            CallInEachModule("Exit", Garbage.a_object_0);
        }

        public static void LoadEssentials() {
            CallInEachModule("LoadEssentials", Garbage.a_object_0);
            LoadedEssentials = true;
        }

        public static void Preload() {
            CallInEachModule("Preload", Garbage.a_object_0);
            Preloaded = true;
        }

        public static void SaveClear(SaveData saveData) {
            CallInEachModule("SaveClear", Garbage.GetObjectArray(saveData));
        }

        public static void SaveClone(SaveData source, SaveData dest) {
            CallInEachModule("SaveClone", Garbage.GetObjectArray(source, dest));
        }

        public static void SaveRead(SaveData saveData, CrcReader reader) {
            CallInEachModule("SaveRead", Garbage.GetObjectArray(saveData, reader));

            if (EnableBugfixes) {
                saveData.HasFPView = saveData.HasFPView || saveData.HasStereo3D;
            }
        }

        public static void SaveWrite(SaveData saveData, CrcWriter writer) {
            CallInEachModule("SaveWrite", Garbage.GetObjectArray(saveData, writer));
        }

        public static string ProcessLevelName(string levelName) {
            return CallInEachModule<string>("ProcessLevelName", levelName);
        }

        public static void ProcessLevelData(Level levelData) {
            CallInEachModule("ProcessLevelData", Garbage.GetObjectArray(levelData));
        }

        public static void HandleCrash(Exception e) {
            CallInEachModule("HandleCrash", Garbage.GetObjectArray(e));
        }

        //Additional methods
        //...

        //Helper methods
        private static void CallInEachModule(string methodName, object[] args) {
            Type[] argsTypes = null;
            for (int i = 0; i < ModuleTypes.Length; i++) {
                Dictionary<string, MethodInfo> moduleMethods = ModuleMethods[i];
                MethodInfo method;
                if (moduleMethods.TryGetValue(methodName, out method)) {
                    if (method == null) {
                        continue;
                    }
                    Common.ReflectionHelper.InvokeMethod(method, Modules[i], args);
                    continue;
                }
                
                if (argsTypes == null) {
                    argsTypes = Type.GetTypeArray(args);
                }
                method = ModuleTypes[i].GetMethod(methodName, argsTypes);
                moduleMethods[methodName] = method;
                if (method == null) {
                    continue;
                }
                Common.ReflectionHelper.InvokeMethod(method, Modules[i], args);
            }
        }

        private static T CallInEachModule<T>(string methodName, T arg) {
            Type[] argsTypes = { typeof(T) };
            object[] args = { arg };
            for (int i = 0; i < Modules.Count; i++) {
                FezModuleCore module = Modules[i];
                //TODO use module method cache
                MethodInfo method = module.GetType().GetMethod(methodName, argsTypes);
                if (method == null) {
                    continue;
                }
                arg = (T) Common.ReflectionHelper.InvokeMethod(method, module, args);
            }
            return arg;
        }

    }
}

