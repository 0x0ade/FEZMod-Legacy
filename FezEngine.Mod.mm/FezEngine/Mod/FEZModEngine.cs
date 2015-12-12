using System;
using System.Collections.Generic;
using System.IO;
using FezEngine.Structure;
using Microsoft.Xna.Framework;
using FezEngine.Services;
using FezEngine.Tools;
using System.Reflection;
using Common;
using ContentSerialization;
using ContentSerialization.Attributes;

namespace FezEngine.Mod {
    
    public abstract class FezModuleSettings {
        [Serialization(Ignore = true)]
        public string FileDefault;
        
        public FezModuleSettings()
            : this(null) {
        }
        
        public FezModuleSettings(string fileDefault) {
            FileDefault = fileDefault;
        }
        
        public static T Load<T>(string file, T alt) where T : FezModuleSettings {
            if (!File.Exists(file)) {
                alt.FileDefault = file;
                return alt;
            }
            
            T settings;
            try {
                settings = SdlSerializer.Deserialize<T>(file) ?? alt;
            } catch {
                settings = alt;
            }
            settings.FileDefault = file;
            return settings;
        }
    }
    public static class FezModuleSettingsExtensions {
        public static void Save<T>(this T settings, string file = null) where T : FezModuleSettings {
            file = file ?? settings.FileDefault;

            SdlSerializer.Serialize<T>(file, settings);
        }
    }
    public abstract class FezModuleCore {

        public abstract string Name { get; }
        public abstract string Author { get; }
        public abstract string Version { get; }

        public FezModuleCore() {
        }

        public virtual void PreInitialize() {}
        public virtual void ParseArgs(string[] args) {}
        public virtual void Initialize() {}
        //public virtual void InitializeMenu(MenuBase mb) {}
        //public virtual void LoadComponents(Fez game) {}
        public virtual void Exit() {}
        public virtual void HandleCrash(Exception e) {}
        public virtual void LoadEssentials() {}
        public virtual void Preload() {}
        //public virtual void SaveClear(SaveData saveData) {}
        //public virtual void SaveClone(SaveData source, SaveData dest) {}
        //public virtual void SaveRead(SaveData saveData, CrcReader reader) {}
        //public virtual void SaveWrite(SaveData saveData, CrcWriter writer) {}
        public virtual string ProcessLevelName(string levelName) {return levelName;}
        public virtual void ProcessLevelData(Level levelData) {}

    }
    
    public class FEZModSettings : FezModuleSettings {
        public string LastVersion = FEZModEngine.MODVersion.ToString();
        public string LastFEZVersion = FEZModEngine.FEZVersion.ToString();
        
        public MusicCacheMode MusicCache = MusicCacheMode.Default;
        public bool CacheDisabled = false;
        
        public FEZModSettings()
            : this(null) {
        }
        
        public FEZModSettings(string fileDefault)
            : base(fileDefault) {
        }
    }
    public class FEZModEngine : FezModuleCore {
        
        public static Func<string> GetVersion;
        public static Action<string> SetVersion;
        public static Func<Version> GetMODVersion;
        public static Action<Version> SetMODVersion;
        public static Func<Version> GetFEZVersion;
        public static Action<Version> SetFEZVersion;
        public static Action PassLoadEssentials;
        public static Action PassPreload;
        public static Action PassInitialize;
        
        public static string MODVersionString {
            get {
                return GetVersion();
            }
            set {
                SetVersion(value);
            }
        }
        public static Version MODVersion {
            get {
                return GetMODVersion();
            }
            set {
                SetMODVersion(value);
            }
        }
        public static Version FEZVersion {
            get {
                return GetFEZVersion();
            }
            set {
                SetFEZVersion(value);
            }
        }

        public override string Name { get { return "FEZMod.Engine"; } }
        public override string Author { get { return "AngelDE98 & FEZMod contributors"; } }
        public override string Version { get { return MODVersionString; } }
        
        public static GameTime UpdateGameTime;
        public static GameTime DrawGameTime;
        public static bool OverrideCultureManuallyBecauseMonoIsA_____ = false;
        
        public static bool GetComponentsAsServices = false;
        public static bool HandleComponents = false;
        
        public static FEZModSettings Settings;
        
        public static bool EnablePPHD = false;
        public static bool DumpResources = false;
        public static bool DumpAllResources = false;
        
        
        public static Dictionary<string, Tuple<string, long, int>> AssetMetadata = new Dictionary<string, Tuple<string, long, int>>();

        public FEZModEngine() {
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
                    ModLogger.Log("FEZMod.Engine", "REMOVED - DEAL WITH IT!");
                }
            }
        }
        
    }
}

