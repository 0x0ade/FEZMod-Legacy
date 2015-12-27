#pragma warning disable 436
using System;
using FezGame.Mod;
using FezEngine.Mod;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework.Content;
using System.IO;
using System.Xml;
using MonoMod;
using FmbLib;

namespace FezGame.Services {
	public class GameLevelManager : LevelManager {

        public Level oldLevel;

        public IGameStateManager GameState { [MonoModIgnore] get { return null; } }

        protected Level tmpLevel;

        public extern void orig_Load(string levelName);
        public void Load(string levelName) {
            if (tmpLevel != null) {
                ClearArtSatellites();

                oldLevel = levelData;
                levelData = tmpLevel;
                tmpLevel = null;

                levelData.OnDeserialization();
                return;
            }

            levelName = FEZMod.ProcessLevelName(levelName);
            ContentManager cm = CMProvider.GetForLevel(levelName);
            
            string filePath_ = ("Resources\\levels\\"+(levelName.ToLower())).Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString())+".";

            string filePathFMB = filePath_ + "fmb";
            if (File.Exists(filePathFMB)) {
                ModLogger.Log("FEZMod", "Loading level from FMB: "+levelName);
                
                ClearArtSatellites();
                oldLevel = levelData;
                
                levelData = (Level) FmbUtil.ReadObject(filePathFMB);
                
                if (levelData.SkyName != null) {
                    levelData.Sky = cm.Load<Sky>("Skies/" + levelData.SkyName);
                }
                if (levelData.TrileSetName != null) {
                    levelData.TrileSet = cm.Load<TrileSet>("Trile Sets/" + levelData.TrileSetName);
                }
                if (levelData.SongName != null) {
                    levelData.Song = cm.Load<TrackedSong>("Music/" + levelData.SongName);
                    levelData.Song.Initialize();
                }
                
                FEZMod.ProcessLevelData(levelData);
                GameLevelManagerHelper.Level = levelData;
                return;
            }

            string filePathXML = filePath_ + "xml";
            if (!File.Exists(filePathXML)) {
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

            FileStream fis = new FileStream(filePathXML, FileMode.Open);
            XmlReader xmlReader = XmlReader.Create(fis);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(xmlReader);
            xmlReader.Close();
            fis.Close();

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
                
                FmbUtil.WriteObject(filePath, levelData);
                
                GC.Collect(3);
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

        [MonoModIgnore] public extern void ChangeLevel(string levelName);
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

