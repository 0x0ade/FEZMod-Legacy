using System;
using System.Collections.Generic;
using FezEngine.Structure;
using Microsoft.Xna.Framework;
using FezEngine.Services;
using FezEngine.Tools;
using System.Reflection;
using Common;

namespace FezEngine.Mod {
    
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
        
        public static MusicCacheMode MusicCache = MusicCacheMode.Default;
        public static bool EnablePPHD = false;
        public static bool DumpResources = false;
        public static bool DumpAllResources = false;
        public static bool CacheDisabled = false;
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
                    ModLogger.Log("FEZMod.Engine", "OBSOLETE!");
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

