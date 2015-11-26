using System;
using FezGame.Mod;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework.Audio;
using FezEngine.Mod;
#if FNA
using FezEngine.Structure;
#endif

namespace FezEngine.Services {
    public class SoundManager {

        private bool initialized;
        private string MusicTempDir;
        private Dictionary<string, string> MusicAliases;
        
        private Dictionary<string, byte[]> MusicCache;

        public extern void orig_InitializeLibrary();
        public void InitializeLibrary() {
            if (!FezEngineMod.MusicExtractDisabled && !FezEngineMod.MusicExtractCustom && FezEngineMod.MusicCache == MusicCacheMode.Default) {
                orig_InitializeLibrary();
                return;
            }
            
            if (FezEngineMod.MusicCache == MusicCacheMode.Enabled) {
                #if FNA
                //1.12's default behaviour is to cache.
                orig_InitializeLibrary();
                return;
                #endif
                //Implement caching for 1.11
                
                if (initialized) {
				    return;
                }
                initialized = true;
                using (FileStream packStream = File.OpenRead(Path.Combine("Content", "Music.pak"))) {
                    using (BinaryReader packReader = new BinaryReader(packStream)) {
                        int count = packReader.ReadInt32();
                        MusicCache = new Dictionary<string, byte[]>(count);
                        for (int i = 0; i < count; i++) {
                            string name = packReader.ReadString();
                            int length = packReader.ReadInt32();
                            if (MusicCache.ContainsKey(name)) {
                                ModLogger.Log("FEZMod.SoundManager", "Skipped " + name + " track because it was already loaded");
                                packStream.Seek(length, SeekOrigin.Current);
                            } else {
                                MusicCache.Add(name, packReader.ReadBytes(length));
                            }
                        }
                    }
                }
                
                return;
            } else if (FezEngineMod.MusicCache == MusicCacheMode.Disabled) {
                //Skip caching / extracting completely.
                return;
            }
            
            #if FNA
            throw new Exception("Can't initialize FEZ 1.12+ music library by extracting it (either custom or disabled)!\nUse MusicCacheMode instead!");
            #endif

            string root;
            if (Environment.OSVersion.Platform == PlatformID.MacOSX || Directory.Exists("/Users/")) {
                string environmentVariable = Environment.GetEnvironmentVariable("HOME");
                if (string.IsNullOrEmpty(environmentVariable)) {
                    root = "./FEZ";
                } else {
                    root = environmentVariable + "/Library/Application Support/FEZ";
                }
            } else {
                if (Environment.OSVersion.Platform == PlatformID.Unix) {
                    string environmentVariable2 = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                    if (string.IsNullOrEmpty(environmentVariable2)) {
                        environmentVariable2 = Environment.GetEnvironmentVariable("HOME");
                        if (string.IsNullOrEmpty(environmentVariable2)) {
                            root = "./FEZ";
                        } else {
                            root = environmentVariable2 + "/.local/share/FEZ";
                        }
                    } else {
                        root = environmentVariable2 + "/FEZ";
                    }
                } else {
                    root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FEZ");
                }
            }
            if (!FezEngineMod.MusicExtractDisabled) {
                if (!Directory.Exists(root)) {
                    Directory.CreateDirectory(root);
                }
                while (!Directory.Exists(root)) {
                    Thread.Sleep(0);
                }
                MusicTempDir = Path.Combine(root, "3rcqng1i.djk");
                if (Directory.Exists(MusicTempDir)) {
                    Directory.Delete(MusicTempDir, true);
                }
                while (Directory.Exists(MusicTempDir)) {
                    Thread.Sleep(0);
                }
                Directory.CreateDirectory(MusicTempDir);
                while (!Directory.Exists(MusicTempDir)) {
                    Thread.Sleep(0);
                }
            } else {
                MusicTempDir = Path.Combine(root, "3rcqng1i.djk");
            }
            using (FileStream packStream = File.OpenRead(Path.Combine("Content", "Music.pak"))) {
                using (BinaryReader packReader = new BinaryReader(packStream)) {
                    int count = packReader.ReadInt32();
                    MusicAliases = new Dictionary<string, string>(count);
                    for (int i = 0; i < count; i++) {
                        string name = packReader.ReadString();
                        int length = packReader.ReadInt32();
                        string file = Path.Combine(MusicTempDir, name);
                        if (!FezEngineMod.MusicExtractDisabled) {
                            using (FileStream fileStream = File.Create(file)) {
                                fileStream.Write(packReader.ReadBytes(length), 0, length);
                            }
                        } else {
                            packStream.Seek(length, SeekOrigin.Current);
                        }
                        if (MusicAliases.ContainsKey(name)) {
                            ModLogger.Log("SoundManager", "Skipped " + name + " track because it was already loaded");
                        } else {
                            MusicAliases.Add(name, file);
                        }
                    }
                }
            }
        }

        public extern OggStream orig_GetCue(string name, bool asyncPrecache = false);
        public OggStream GetCue(string name, bool asyncPrecache = false) {
            bool isAmbience = name.Contains("Ambience");

            string oggFile = ((isAmbience ? "" : "music/") + name.Replace(" ^ ", "\\")).Externalize() + ".ogg";
            if (File.Exists(oggFile)) {
                OggStream oggStream = null;
                try {
                    //TODO use the MusicCache
                    #if FNA
                    oggStream = new OggStream(oggFile) {
                    #else
                    oggStream = new OggStream(oggFile, 6) {
                    #endif
                        Category = isAmbience ? "Ambience" : "Music",
                        IsLooped = isAmbience
                    };
                    oggStream.RealName = name;
                    #if !FNA
                    oggStream.Prepare(asyncPrecache);
                    #endif
                    if (name.Contains("Gomez")) {
                        oggStream.LowPass = false;
                    }
                } catch (Exception ex) {
                    ModLogger.Log("FEZMod.SoundManager", ex.Message);
                }
                return oggStream;
            }
            
            //Backport the MusicCache
            if (FezEngineMod.MusicCache != MusicCacheMode.Default) {
                OggStream oggStream = null;
                try {
                    if (FezEngineMod.MusicCache == MusicCacheMode.Enabled) {
                        byte[] data = MusicCache[name.Replace(" ^ ", "\\").ToLowerInvariant()];
                        oggStream = new OggStream(new MemoryStream(data, 0, data.Length, false, true));
                    } else {
                        //Caching is enabled by default since 1.12, so we need to force-disable it here.
                        //It also may be benificial for 1.11- to skip extracting the oggs completely.
                        
                        string findName = name.Replace(" ^ ", "\\").ToLowerInvariant();
                        FileStream packStream = File.OpenRead(Path.Combine("Content", "Music.pak"));
                        BinaryReader packReader = new BinaryReader(packStream);
                        int count = packReader.ReadInt32();
                        for (int i = 0; i < count; i++) {
                            string packName = packReader.ReadString();
                            int length = packReader.ReadInt32();
                            if (findName == packName) {
                                oggStream = new OggStream(new LimitedStream(packStream, packStream.Position, length));
                                break;
                            } else {
                                packStream.Seek(length, SeekOrigin.Current);
                            }
                        }
                        if (oggStream == null) {
                            ModLogger.Log("FEZMod.SoundManager", "Music cue not found: " + name);
                            packReader.Close();
                            packStream.Close();
                        }
                    }
                    
                    oggStream.Category = isAmbience ? "Ambience" : "Music";
                    oggStream.IsLooped = isAmbience;
                    oggStream.RealName = name;
                    #if !FNA
                    oggStream.Prepare(asyncPrecache);
                    #endif
                    if (name.Contains("Gomez")) {
                        oggStream.LowPass = false;
                    }
                } catch (Exception ex) {
                    ModLogger.Log("FEZMod.SoundManager", ex.ToString());
                }
                return oggStream;
            }

            return orig_GetCue(name, asyncPrecache);
        }

    }
}

