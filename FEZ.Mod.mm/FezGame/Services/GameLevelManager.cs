using System;
using FezGame.Mod;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using FezGame.Mod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Reflection;
using MonoMod;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace FezGame.Services {
	public class GameLevelManager : LevelManager {

        public Level oldLevel;

        public IGameStateManager GameState { [MonoModIgnore] get { return null; } }

        protected Level tmpLevel;

        public void orig_Load(string levelName) {
        }

        public void Load(string levelName) {
            if (levelName.StartsWith("JAFM_WORKAROUND_SAVE:")) {
                string[] split = levelName.Split(new char[] {':'});
                Save(split[1], (split.Length > 2) ? bool.Parse(split[2]) : false);
                return;
            }
            if (levelName == "JAFM_WORKAROUND_CHANGELEVEL") {
                ChangeLevel(GameLevelManagerHelper.ChangeLevel_);
                return;
            }

            if (tmpLevel != null) {
                ClearArtSatellites();

                oldLevel = levelData;
                levelData = tmpLevel;
                tmpLevel = null;

                levelData.OnDeserialization();
                return;
            }

            levelName = FEZMod.ProcessLevelName(levelName);

            string filePath_ = ("Resources\\levels\\"+(levelName.ToLower())).Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString())+".";

            string filePathFMB = filePath_ + "fmb";
            FileInfo fileFMB = new FileInfo(filePathFMB);
            if (fileFMB.Exists) {
                ModLogger.Log("FEZMod", "Loading level from FMB: "+levelName);

                using (FileStream fs = new FileStream(fileFMB.FullName, FileMode.Open)) {
                    throw new FormatException("FEZMod can't load FMB files yet.");
                }

                return;
            }

            string filePathXML = filePath_+"xml";
            FileInfo fileXML = new FileInfo(filePathXML);
            if (!fileXML.Exists) {
                if (MemoryContentManager.AssetExists("LEVELS/"+levelName)) {
                    orig_Load(levelName);
                } else {
                    //Fallback: Spawn at the original VILLAGEVILLE_3D if the level wasn't found at all
                    ModLogger.Log("FEZMod", "Level not found: " + levelName + "; Falling back to the original VILLAGEVILLE_3D...");
                    orig_Load("VILLAGEVILLE_3D");
                }
                FEZMod.ProcessLevelData(levelData);
                GameLevelManagerHelper.Level = levelData;
                return;
            }

            ModLogger.Log("FEZMod", "Loading level from XML: "+levelName);

            ClearArtSatellites();

            oldLevel = levelData;

            FileStream fis = new FileStream(fileXML.FullName, FileMode.Open);
            XmlReader xmlReader = XmlReader.Create(fis);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(xmlReader);
            xmlReader.Close();
            fis.Close();

            ContentManager cm = CMProvider.GetForLevel(levelName);
            levelData = (Level) xmlDocument.Deserialize(null, cm, true);
            levelData.Name = levelName;

            LevelSaveData save = GameState.SaveData.ThisLevel;
            if (save != null) {
                save.FirstVisit = false;
            }

            FEZMod.ProcessLevelData(levelData);
            GameLevelManagerHelper.Level = levelData;
        }

        public void Save(string levelName, bool binary = false) {
            string filePath = ("Resources\\levels\\"+(levelName.ToLower())).Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString())+"."+(binary?"fmb":"xml");
            FileInfo file = new FileInfo(filePath);
            if (file.Exists) {
                return;
            }

            if (binary) {
                ModLogger.Log("FEZMod", "Saving level to binary file: " + levelName);

                //TODO use custom writer instead of quickly and dirtily serializing the level
                //FYI: Levels are not serializable.
                using (FileStream fs = new FileStream(filePath, FileMode.CreateNew)) {
                    throw new FormatException("FEZMod can't save FMB files yet.");
                }

                return;
            }

            ModLogger.Log("FEZMod", "Saving level to XML: "+levelName);

            XmlDocument xmlDocument = new XmlDocument();

            xmlDocument.AppendChild(levelData.Serialize(xmlDocument));

            //Write to file
            FileStream fos = new FileStream(filePath, FileMode.CreateNew);
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;
            XmlWriter xmlWriter = XmlWriter.Create(fos, xmlWriterSettings);
            xmlDocument.Save(xmlWriter);
            xmlWriter.Close();
            fos.Close();

            GC.Collect(3);
        }

        [MonoModIgnore]
        public void ChangeLevel(string levelName) {
        }

        public void ChangeLevel(Level level) {
            ContentManager cm = CMProvider.GetForLevel(level.Name);
            if (level.SkyName != null) {
                level.Sky = cm.Load<Sky>("Skies/" + level.SkyName);
            }
            if (level.TrileSetName != null) {
                level.TrileSet = cm.Load<TrileSet>("Trile Sets/" + level.TrileSetName);
            }
            if (level.SongName != null) {
                level.Song = cm.Load<TrackedSong>("Music/" + level.SongName);
                level.Song.Initialize();
            }

            tmpLevel = level;
            ChangeLevel(level.Name);

            GameLevelManagerHelper.Level = level;
        }

    }
}

