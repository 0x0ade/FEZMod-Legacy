using System;
using Common;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework.Audio;
using FezEngine.Tools;

namespace FezEngine.Services {
    public class SoundManager {

        public static bool ExtractCustom = false;
        public static bool ExtractDisabled = false;

        private bool initialized;
        private string MusicTempDir;
        private Dictionary<string, string> MusicAliases;

        public void orig_InitializeLibrary() {
        }

        public void InitializeLibrary() {
            if (!ExtractDisabled && !ExtractCustom) {
                orig_InitializeLibrary();
                return;
            }

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
            if (!ExtractDisabled) {
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
                        if (!ExtractDisabled) {
                            using (FileStream fileStream = File.Create(file)) {
                                fileStream.Write(packReader.ReadBytes(length), 0, length);
                            }
                        } else {
                            packReader.ReadBytes(length);
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

        public OggStream orig_GetCue(string name, bool asyncPrecache = false) {
            return null;
        }

        public OggStream GetCue(string name, bool asyncPrecache = false) {
            bool isAmbience = name.Contains("Ambience");

            string oggFile = MemoryContentManager.Externalize((isAmbience ? "" : "music/") + name.Replace(" ^ ", "\\")) + ".ogg";
            ModLogger.Log("FEZMod.SoundManager", oggFile);
            if (File.Exists(oggFile)) {
                OggStream oggStream = (OggStream) null;
                try {
                    oggStream = new OggStream(oggFile, 6) {
                        Category = isAmbience ? "Ambience" : "Music",
                        IsLooped = isAmbience
                    };
                    oggStream.RealName = name;
                    oggStream.Prepare(asyncPrecache);
                    if (name.Contains("Gomez")) {
                        oggStream.LowPass = false;
                    }
                } catch (Exception ex) {
                    ModLogger.Log("FEZMod.SoundManager", ex.Message);
                }
                return oggStream;
            }

            return orig_GetCue(name, asyncPrecache);
        }

    }
}

