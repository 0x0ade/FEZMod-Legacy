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
            if (FezEngineMod.MusicCache == MusicCacheMode.Default) {
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

