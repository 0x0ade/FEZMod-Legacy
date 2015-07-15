using System;
using Common;
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

            //levelName = ProcessLevelName(levelName);

            string filePath_ = ("Resources\\levels\\"+(levelName.ToLower())).Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString())+".";

            string filePathFML = filePath_ + FEZMod.LevelFileFormat;
            FileInfo fileFML = new FileInfo(filePathFML);
            if (fileFML.Exists) {
                ModLogger.Log("FEZMod", "Loading level from FML: "+levelName);

                using (FileStream fs = new FileStream(fileFML.FullName, FileMode.Open)) {
                    throw new FormatException("FEZMod can't load FML files yet.");
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
                //ProcessLevelData(levelData);
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
            XmlElement xmlLevel = xmlDocument["Level"];

            ContentManager cm = CMProvider.GetForLevel(levelName);
            levelData = (Level) xmlLevel.Deserialize(null, cm, true);
            levelData.Name = levelName;

            LevelSaveData save = GameState.SaveData.ThisLevel;
            if (save != null) {
                save.FirstVisit = false;
            }

            //ProcessLevelData(levelData);
            GameLevelManagerHelper.Level = levelData;
        }

        public void Save(string levelName, bool binary = false) {
            string filePath = ("Resources\\levels\\"+(levelName.ToLower())).Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString())+"."+(binary?FEZMod.LevelFileFormat:"xml");
            FileInfo file = new FileInfo(filePath);
            if (file.Exists) {
                return;
            }

            if (binary) {
                ModLogger.Log("FEZMod", "Saving level to binary file: " + levelName);

                //TODO use custom writer instead of quickly and dirtily serializing the level
                //FYI: Levels are not serializable.
                using (FileStream fs = new FileStream(filePath, FileMode.CreateNew)) {
                    throw new FormatException("FEZMod can't save FML files yet.");
                }

                return;
            }

            ModLogger.Log("FEZMod", "Saving level to XML: "+levelName);

            //TODO Use XmlHelper.Serialize... if it would exist.

            //ANCIENT CODE; DO NOT TOUCH AS LONG AS IT IS WORKING.
            //Nobody knows what really is going on here and it should be completely replaced with much simpler code.

            XmlDocument xmlDocument = new XmlDocument();

            XmlElement xmlLevel = xmlDocument.CreateElement("Level");

            //Level attributes
            MethodInfo[] levelMethods = levelData.GetType().GetMethods();
            for (int i = 0; i < levelMethods.Length; i++) {
                MethodInfo method = levelMethods[i];
                string methodLowerCase = method.Name.ToLower();
                if (!methodLowerCase.StartsWith("get_")) {
                    continue;
                }

                Type returnType = method.ReturnType;

                if (!(
                    typeof(ValueType).IsAssignableFrom(returnType) ||
                    typeof(Enum).IsAssignableFrom(returnType) ||
                    typeof(string).IsAssignableFrom(returnType)
                )) {
                    continue;
                }

                string attributeName = methodLowerCase.Substring(4);
                object value = method.Invoke(levelData, new object[0]);
                if (value != null) {
                    xmlLevel.SetAttribute(attributeName, value.ToString());
                }
            }

            //Level content
            XmlElement xmlSize = xmlDocument.CreateElement("Size");
            XmlElement xmlSizeV3 = xmlDocument.CreateElement("Vector3");
            Vector3 size = levelData.Size;
            xmlSizeV3.SetAttribute("x", size.X.ToString());
            xmlSizeV3.SetAttribute("y", size.Y.ToString());
            xmlSizeV3.SetAttribute("z", size.Z.ToString());
            xmlSize.AppendChild(xmlSizeV3);
            xmlLevel.AppendChild(xmlSize);

            XmlElement xmlSpawn = xmlDocument.CreateElement("StartingPosition");
            XmlElement xmlSpawnFace = xmlDocument.CreateElement("TrileFace");
            TrileFace spawn = levelData.StartingPosition;
            xmlSpawnFace.SetAttribute("face", spawn.Face.ToString());
            XmlElement xmlSpawnTrile = xmlDocument.CreateElement("TrileId");
            XmlElement xmlSpawnTrileEmplacement = xmlDocument.CreateElement("TrileEmplacement");
            xmlSpawnTrileEmplacement.SetAttribute("x", spawn.Id.X.ToString());
            xmlSpawnTrileEmplacement.SetAttribute("y", spawn.Id.Y.ToString());
            xmlSpawnTrileEmplacement.SetAttribute("z", spawn.Id.Z.ToString());
            xmlSpawnTrile.AppendChild(xmlSpawnTrileEmplacement);
            xmlSpawnFace.AppendChild(xmlSpawnTrile);
            xmlSpawn.AppendChild(xmlSpawnFace);
            xmlLevel.AppendChild(xmlSpawn);

            XmlElement xmlVolumes = xmlDocument.CreateElement("Volumes");
            foreach (int key in levelData.Volumes.Keys) {
                if (key < 0) {
                    continue;
                }

                Volume volume = levelData.Volumes[key];

                XmlElement xmlVolumeEntry = xmlDocument.CreateElement("Entry");
                xmlVolumeEntry.SetAttribute("key", key.ToString());
                XmlElement xmlVolume = xmlDocument.CreateElement("Volume");

                XmlElement xmlOrientations = xmlDocument.CreateElement("Orientations");
                foreach (FaceOrientation volumeOrientation in volume.Orientations) {
                    XmlElement xmlOrientation = xmlDocument.CreateElement("FaceOrientation");
                    xmlOrientation.InnerText = volumeOrientation.ToString();
                    xmlOrientations.AppendChild(xmlOrientation);
                }
                xmlVolume.AppendChild(xmlOrientations);

                XmlElement xmlFrom = xmlDocument.CreateElement("From");
                XmlElement xmlFromV3 = xmlDocument.CreateElement("Vector3");
                xmlFromV3.SetAttribute("x", volume.From.X.ToString());
                xmlFromV3.SetAttribute("y", volume.From.Y.ToString());
                xmlFromV3.SetAttribute("z", volume.From.Z.ToString());
                xmlFrom.AppendChild(xmlFromV3);
                xmlVolume.AppendChild(xmlFrom);

                XmlElement xmlTo = xmlDocument.CreateElement("To");
                XmlElement xmlToV3 = xmlDocument.CreateElement("Vector3");
                xmlToV3.SetAttribute("x", volume.To.X.ToString());
                xmlToV3.SetAttribute("y", volume.To.Y.ToString());
                xmlToV3.SetAttribute("z", volume.To.Z.ToString());
                xmlTo.AppendChild(xmlToV3);
                xmlVolume.AppendChild(xmlTo);

                xmlVolumeEntry.AppendChild(xmlVolume);
                xmlVolumes.AppendChild(xmlVolumeEntry);
            }
            xmlLevel.AppendChild(xmlVolumes);

            XmlElement xmlScripts = xmlDocument.CreateElement("Scripts");
            foreach (int key in levelData.Scripts.Keys) {
                if (key < 0) {
                    continue;
                }

                XmlElement xmlScriptEntry = xmlDocument.CreateElement("Entry");
                xmlScriptEntry.SetAttribute("key", key.ToString());
                XmlElement xmlScript = xmlDocument.CreateElement("Script");
                Script script = levelData.Scripts[key];

                xmlScript.SetAttribute("name", script.Name);
                xmlScript.SetAttribute("oneTime", script.OneTime.ToString());
                xmlScript.SetAttribute("triggerless", script.Triggerless.ToString());
                xmlScript.SetAttribute("ignoreEndTriggers", script.IgnoreEndTriggers.ToString());
                xmlScript.SetAttribute("levelWideOneTime", script.LevelWideOneTime.ToString());
                xmlScript.SetAttribute("disabled", script.Disabled.ToString());
                xmlScript.SetAttribute("isWinCondition", script.IsWinCondition.ToString());

                XmlElement xmlTriggers = xmlDocument.CreateElement("Triggers");
                for (int ii = 0; ii < script.Triggers.Count; ii++) {
                    XmlElement xmlTrigger = xmlDocument.CreateElement("ScriptTrigger");
                    ScriptTrigger trigger = script.Triggers[ii];

                    xmlTrigger.SetAttribute("event", trigger.Event);

                    XmlElement xmlEntity = xmlDocument.CreateElement("Entity");
                    Entity entity = trigger.Object;
                    xmlEntity.SetAttribute("entityType", entity.Type);
                    Nullable<int> entityID = entity.Identifier;
                    if (entityID != null && entityID.HasValue) {
                        xmlEntity.SetAttribute("identifier", entityID.Value.ToString());
                    }
                    xmlTrigger.AppendChild(xmlEntity);

                    xmlTriggers.AppendChild(xmlTrigger);
                }
                xmlScript.AppendChild(xmlTriggers);

                XmlElement xmlActions = xmlDocument.CreateElement("Actions");
                for (int ii = 0; ii < script.Actions.Count; ii++) {
                    XmlElement xmlAction = xmlDocument.CreateElement("ScriptAction");
                    ScriptAction action = script.Actions[ii];

                    xmlAction.SetAttribute("operation", action.Operation);
                    xmlAction.SetAttribute("killswitch", action.Killswitch.ToString());
                    xmlAction.SetAttribute("blocking", action.Blocking.ToString());

                    XmlElement xmlEntity = xmlDocument.CreateElement("Entity");
                    Entity entity = action.Object;
                    xmlEntity.SetAttribute("entityType", entity.Type);
                    Nullable<int> entityID = entity.Identifier;
                    if (entityID != null && entityID.HasValue) {
                        xmlEntity.SetAttribute("identifier", entityID.Value.ToString());
                    }
                    xmlAction.AppendChild(xmlEntity);

                    XmlElement xmlArguments = xmlDocument.CreateElement("Arguments");
                    String[] arguments = action.Arguments;
                    for (int iii = 0; iii < arguments.Length; iii++) {
                        XmlElement xmlArgument = xmlDocument.CreateElement("Entry");
                        xmlArgument.InnerText = arguments[iii];
                        xmlArguments.AppendChild(xmlArgument);
                    }
                    xmlAction.AppendChild(xmlArguments);

                    xmlActions.AppendChild(xmlAction);
                }
                xmlScript.AppendChild(xmlActions);

                xmlScriptEntry.AppendChild(xmlScript);
                xmlScripts.AppendChild(xmlScriptEntry);
            }
            xmlLevel.AppendChild(xmlScripts);

            XmlElement xmlTriles = xmlDocument.CreateElement("Triles");
            foreach (TrileEmplacement trileEmplacement in levelData.Triles.Keys) {
                XmlElement xmlTrileEntry = xmlDocument.CreateElement("Entry");

                XmlElement xmlTrileEmplacement = xmlDocument.CreateElement("TrileEmplacement");
                xmlTrileEmplacement.SetAttribute("x", trileEmplacement.X.ToString());
                xmlTrileEmplacement.SetAttribute("y", trileEmplacement.Y.ToString());
                xmlTrileEmplacement.SetAttribute("z", trileEmplacement.Z.ToString());
                xmlTrileEntry.AppendChild(xmlTrileEmplacement);

                TrileInstance trile = levelData.Triles[trileEmplacement];

                XmlElement xmlTrile = xmlDocument.CreateElement("TrileInstance");
                xmlTrile.SetAttribute("trileId", trile.TrileId.ToString());

                XmlElement xmlTrilePosition = xmlDocument.CreateElement("Position");
                XmlElement xmlTrilePositionV3 = xmlDocument.CreateElement("Vector3");
                xmlTrilePositionV3.SetAttribute("x", trile.Position.X.ToString());
                xmlTrilePositionV3.SetAttribute("y", trile.Position.Y.ToString());
                xmlTrilePositionV3.SetAttribute("z", trile.Position.Z.ToString());
                xmlTrilePosition.AppendChild(xmlTrilePositionV3);
                xmlTrile.AppendChild(xmlTrilePosition);

                //this.data.PositionPhi.W = (float) ((int) orientation - 2) * 1.570796f;
                xmlTrile.SetAttribute("orientation", ((int) Math.Round((((double)trile.Data.PositionPhi.W) / 1.570796D) + 2D)).ToString());
                
                xmlTrileEntry.AppendChild(xmlTrile);
                xmlTriles.AppendChild(xmlTrileEntry);
            }
            xmlLevel.AppendChild(xmlTriles);

            XmlElement xmlAOs = xmlDocument.CreateElement("ArtObjects");
            foreach (int key in levelData.ArtObjects.Keys) {
                if (key < 0) {
                    continue;
                }

                XmlElement xmlAOEntry = xmlDocument.CreateElement("Entry");
                xmlAOEntry.SetAttribute("key", key.ToString());

                XmlElement xmlAO = xmlDocument.CreateElement("ArtObjectInstance");
                ArtObjectInstance ao = levelData.ArtObjects[key];

                xmlAO.SetAttribute("name", ao.ArtObjectName);

                XmlElement xmlAOPosition = xmlDocument.CreateElement("Position");
                XmlElement xmlAOPositionV3 = xmlDocument.CreateElement("Vector3");
                xmlAOPositionV3.SetAttribute("x", ao.Position.X.ToString());
                xmlAOPositionV3.SetAttribute("y", ao.Position.Y.ToString());
                xmlAOPositionV3.SetAttribute("z", ao.Position.Z.ToString());
                xmlAOPosition.AppendChild(xmlAOPositionV3);
                xmlAO.AppendChild(xmlAOPosition);

                XmlElement xmlAORotation = xmlDocument.CreateElement("Rotation");
                XmlElement xmlAORotationQ = xmlDocument.CreateElement("Quaternion");
                xmlAORotationQ.SetAttribute("x", ao.Rotation.X.ToString());
                xmlAORotationQ.SetAttribute("y", ao.Rotation.Y.ToString());
                xmlAORotationQ.SetAttribute("z", ao.Rotation.Z.ToString());
                xmlAORotationQ.SetAttribute("w", ao.Rotation.W.ToString());
                xmlAORotation.AppendChild(xmlAORotationQ);
                xmlAO.AppendChild(xmlAORotation);

                XmlElement xmlAOScale = xmlDocument.CreateElement("Scale");
                XmlElement xmlAOScaleV3 = xmlDocument.CreateElement("Vector3");
                xmlAOScaleV3.SetAttribute("x", ao.Scale.X.ToString());
                xmlAOScaleV3.SetAttribute("y", ao.Scale.Y.ToString());
                xmlAOScaleV3.SetAttribute("z", ao.Scale.Z.ToString());
                xmlAOScale.AppendChild(xmlAOScaleV3);
                xmlAO.AppendChild(xmlAOScale);

                XmlElement xmlAOSettings = xmlDocument.CreateElement("ArtObjectActorSettings");
                ArtObjectActorSettings settings = ao.ActorSettings;

                xmlAOSettings.SetAttribute("inactive", settings.Inactive.ToString());
                xmlAOSettings.SetAttribute("spinEvery", settings.SpinEvery.ToString());
                xmlAOSettings.SetAttribute("spinOffset", settings.SpinOffset.ToString());
                xmlAOSettings.SetAttribute("offCenter", settings.OffCenter.ToString());
                xmlAOSettings.SetAttribute("timeswitchWindBackSpeed", settings.TimeswitchWindBackSpeed.ToString());
                xmlAOSettings.SetAttribute("containedTrile", settings.ContainedTrile.ToString());
                Nullable<int> attatchedGroup = settings.AttachedGroup;
                if (attatchedGroup != null && attatchedGroup.HasValue) {
                    xmlAOSettings.SetAttribute("attatchedGroup", attatchedGroup.Value.ToString());
                }
                xmlAOSettings.SetAttribute("spinView", settings.SpinView.ToString());

                XmlElement xmlAOSettingsRotationCenter = xmlDocument.CreateElement("RotationCenter");
                XmlElement xmlAOSettingsRotationCenterV3 = xmlDocument.CreateElement("Vector3");
                xmlAOSettingsRotationCenterV3.SetAttribute("x", settings.RotationCenter.X.ToString());
                xmlAOSettingsRotationCenterV3.SetAttribute("y", settings.RotationCenter.Y.ToString());
                xmlAOSettingsRotationCenterV3.SetAttribute("z", settings.RotationCenter.Z.ToString());
                xmlAOSettingsRotationCenter.AppendChild(xmlAOSettingsRotationCenterV3);
                xmlAOSettings.AppendChild(xmlAOSettingsRotationCenter);

                xmlAO.AppendChild(xmlAOSettings);

                xmlAOEntry.AppendChild(xmlAO);
                xmlAOs.AppendChild(xmlAOEntry);
            }
            xmlLevel.AppendChild(xmlAOs);

            XmlElement xmlPlanes = xmlDocument.CreateElement("BackgroundPlanes");
            foreach (int key in levelData.BackgroundPlanes.Keys) {
                if (key < 0) {
                    continue;
                }

                BackgroundPlane plane = levelData.BackgroundPlanes[key];

                if (plane.TextureName == null) {
                    continue;
                }

                XmlElement xmlPlaneEntry = xmlDocument.CreateElement("Entry");
                xmlPlaneEntry.SetAttribute("key", key.ToString());
                XmlElement xmlPlane = xmlDocument.CreateElement("BackgroundPlane");

                xmlPlane.SetAttribute("textureName", plane.TextureName);
                xmlPlane.SetAttribute("lightMap", plane.LightMap.ToString());
                xmlPlane.SetAttribute("allowOverbrightness", plane.AllowOverbrightness.ToString());
                xmlPlane.SetAttribute("animated", plane.Animated.ToString());
                xmlPlane.SetAttribute("doubleSided", plane.Doublesided.ToString());
                xmlPlane.SetAttribute("opacity", plane.Opacity.ToString());
                xmlPlane.SetAttribute("billboard", plane.Billboard.ToString());
                xmlPlane.SetAttribute("syncWithSamples", plane.SyncWithSamples.ToString());
                xmlPlane.SetAttribute("crosshatch", plane.Crosshatch.ToString());
                //xmlPlane.SetAttribute("unknown", Boolean.toString(plane.get_Unknown()));//Troll reference XML is troll
                xmlPlane.SetAttribute("alwaysOnTop", plane.AlwaysOnTop.ToString());
                xmlPlane.SetAttribute("fullbright", plane.Fullbright.ToString());
                xmlPlane.SetAttribute("pixelatedLightmap", plane.PixelatedLightmap.ToString());
                xmlPlane.SetAttribute("xTextureRepeat", plane.XTextureRepeat.ToString());
                xmlPlane.SetAttribute("yTextureRepeat", plane.YTextureRepeat.ToString());
                xmlPlane.SetAttribute("clampTexture", plane.ClampTexture.ToString());
                xmlPlane.SetAttribute("parallaxFactor", plane.ParallaxFactor.ToString());
                xmlPlane.SetAttribute("filter", "#"+plane.Filter.PackedValue.ToString("X"));
                xmlPlane.SetAttribute("actorType", plane.ActorType.ToString());

                XmlElement xmlPlanePosition = xmlDocument.CreateElement("Position");
                XmlElement xmlPlanePositionV3 = xmlDocument.CreateElement("Vector3");
                xmlPlanePositionV3.SetAttribute("x", plane.Position.X.ToString());
                xmlPlanePositionV3.SetAttribute("y", plane.Position.Y.ToString());
                xmlPlanePositionV3.SetAttribute("z", plane.Position.Z.ToString());
                xmlPlanePosition.AppendChild(xmlPlanePositionV3);
                xmlPlane.AppendChild(xmlPlanePosition);

                XmlElement xmlPlaneRotation = xmlDocument.CreateElement("Rotation");
                XmlElement xmlPlaneRotationQ = xmlDocument.CreateElement("Quaternion");
                xmlPlaneRotationQ.SetAttribute("x", plane.Rotation.X.ToString());
                xmlPlaneRotationQ.SetAttribute("y", plane.Rotation.Y.ToString());
                xmlPlaneRotationQ.SetAttribute("z", plane.Rotation.Z.ToString());
                xmlPlaneRotationQ.SetAttribute("w", plane.Rotation.W.ToString());
                xmlPlaneRotation.AppendChild(xmlPlaneRotationQ);
                xmlPlane.AppendChild(xmlPlaneRotation);

                XmlElement xmlPlaneScale = xmlDocument.CreateElement("Scale");
                XmlElement xmlPlaneScaleV3 = xmlDocument.CreateElement("Vector3");
                xmlPlaneScaleV3.SetAttribute("x", plane.Scale.X.ToString());
                xmlPlaneScaleV3.SetAttribute("y", plane.Scale.Y.ToString());
                xmlPlaneScaleV3.SetAttribute("z", plane.Scale.Z.ToString());
                xmlPlaneScale.AppendChild(xmlPlaneScaleV3);
                xmlPlane.AppendChild(xmlPlaneScale);

                xmlPlaneEntry.AppendChild(xmlPlane);
                xmlPlanes.AppendChild(xmlPlaneEntry);
            }
            xmlLevel.AppendChild(xmlPlanes);

            XmlElement xmlGroups = xmlDocument.CreateElement("Groups");
            foreach (int key in levelData.Groups.Keys) {
                if (key < 0) {
                    continue;
                }

                XmlElement xmlGroupEntry = xmlDocument.CreateElement("Entry");
                xmlGroupEntry.SetAttribute("key", key.ToString());
                XmlElement xmlGroup = xmlDocument.CreateElement("TrileGroup");

                TrileGroup trileGroup = levelData.Groups[key];

                xmlGroup.SetAttribute("heavy", trileGroup.Heavy.ToString());
                xmlGroup.SetAttribute("geyserOffset", trileGroup.GeyserOffset.ToString());
                xmlGroup.SetAttribute("geyserPauseFor", trileGroup.GeyserPauseFor.ToString());
                xmlGroup.SetAttribute("geyserLiftFor", trileGroup.GeyserLiftFor.ToString());
                xmlGroup.SetAttribute("geyserApexHeight", trileGroup.GeyserApexHeight.ToString());
                xmlGroup.SetAttribute("spinClockwise", trileGroup.SpinClockwise.ToString());
                xmlGroup.SetAttribute("spinFrequency", trileGroup.SpinFrequency.ToString());
                xmlGroup.SetAttribute("spinNeedsTriggering", trileGroup.SpinNeedsTriggering.ToString());
                xmlGroup.SetAttribute("spin180Degrees", trileGroup.Spin180Degrees.ToString());
                xmlGroup.SetAttribute("fallOnRotate", trileGroup.FallOnRotate.ToString());
                xmlGroup.SetAttribute("spinOffset", trileGroup.SpinOffset.ToString());
                xmlGroup.SetAttribute("actorType", trileGroup.ActorType.ToString());

                XmlElement xmlGroupTriles = xmlDocument.CreateElement("Triles");
                for (int ii = 0; ii < trileGroup.Triles.Count; ii++) {
                    TrileInstance groupTrile = trileGroup.Triles[ii];
                    XmlElement xmlGroupTrile = xmlDocument.CreateElement("TrileInstance");
                    xmlGroupTrile.SetAttribute("trileId", groupTrile.TrileId.ToString());

                    XmlElement xmlGroupTrilePosition = xmlDocument.CreateElement("Position");
                    XmlElement xmlGroupTrilePositionV3 = xmlDocument.CreateElement("Vector3");
                    xmlGroupTrilePositionV3.SetAttribute("x", groupTrile.Position.X.ToString());
                    xmlGroupTrilePositionV3.SetAttribute("y", groupTrile.Position.Y.ToString());
                    xmlGroupTrilePositionV3.SetAttribute("z", groupTrile.Position.Z.ToString());
                    xmlGroupTrilePosition.AppendChild(xmlGroupTrilePositionV3);
                    xmlGroupTrile.AppendChild(xmlGroupTrilePosition);

                    //this.data.PositionPhi.W = (float) ((int) orientation - 2) * 1.570796f;
                    xmlGroupTrile.SetAttribute("orientation", ((int) ((groupTrile.Data.PositionPhi.W / 1.570796f) + 2)).ToString());

                    xmlGroupTriles.AppendChild(xmlGroupTrile);
                }
                xmlGroup.AppendChild(xmlGroupTriles);

                XmlElement xmlGroupSpinCenter = xmlDocument.CreateElement("SpinCenter");
                XmlElement xmlGroupSpinCenterV3 = xmlDocument.CreateElement("Vector3");
                xmlGroupSpinCenterV3.SetAttribute("x", trileGroup.SpinCenter.X.ToString());
                xmlGroupSpinCenterV3.SetAttribute("y", trileGroup.SpinCenter.Y.ToString());
                xmlGroupSpinCenterV3.SetAttribute("z", trileGroup.SpinCenter.Z.ToString());
                xmlGroupSpinCenter.AppendChild(xmlGroupSpinCenterV3);
                xmlGroup.AppendChild(xmlGroupSpinCenter);

                xmlGroupEntry.AppendChild(xmlGroup);
                xmlGroups.AppendChild(xmlGroupEntry);
            }
            xmlLevel.AppendChild(xmlGroups);

            XmlElement xmlNPCs = xmlDocument.CreateElement("NonplayerCharacters");
            foreach (int key in levelData.NonPlayerCharacters.Keys) {
                if (key < 0) {
                    continue;
                }

                XmlElement xmlNPCEntry = xmlDocument.CreateElement("Entry");
                xmlNPCEntry.SetAttribute("key", key.ToString());
                XmlElement xmlNPC = xmlDocument.CreateElement("NpcInstance");

                NpcInstance npc = levelData.NonPlayerCharacters[key];
                xmlNPC.SetAttribute("name", npc.Name);
                xmlNPC.SetAttribute("walkSpeed", npc.WalkSpeed.ToString());
                xmlNPC.SetAttribute("randomizeSpeech", npc.RandomizeSpeech.ToString());
                xmlNPC.SetAttribute("sayFirstSpeechLineOnce", npc.SayFirstSpeechLineOnce.ToString());
                xmlNPC.SetAttribute("avoidsGomez", npc.AvoidsGomez.ToString());
                xmlNPC.SetAttribute("actorType", npc.ActorType.ToString());

                XmlElement xmlNPCPosition = xmlDocument.CreateElement("Position");
                XmlElement xmlNPCPositionV3 = xmlDocument.CreateElement("Vector3");
                xmlNPCPositionV3.SetAttribute("x", npc.Position.X.ToString());
                xmlNPCPositionV3.SetAttribute("y", npc.Position.Y.ToString());
                xmlNPCPositionV3.SetAttribute("z", npc.Position.Z.ToString());
                xmlNPCPosition.AppendChild(xmlNPCPositionV3);
                xmlNPC.AppendChild(xmlNPCPosition);

                XmlElement xmlNPCDestinationOffset = xmlDocument.CreateElement("DestinationOffset");
                XmlElement xmlNPCDestinationOffsetV3 = xmlDocument.CreateElement("Vector3");
                xmlNPCDestinationOffsetV3.SetAttribute("x", npc.DestinationOffset.X.ToString());
                xmlNPCDestinationOffsetV3.SetAttribute("y", npc.DestinationOffset.Y.ToString());
                xmlNPCDestinationOffsetV3.SetAttribute("z", npc.DestinationOffset.Z.ToString());
                xmlNPCDestinationOffset.AppendChild(xmlNPCDestinationOffsetV3);
                xmlNPC.AppendChild(xmlNPCDestinationOffset);

                XmlElement xmlSpeech = xmlDocument.CreateElement("Speech");
                for (int ii = 0; ii < npc.Speech.Count; ii++) {
                    XmlElement xmlSpeechLine = xmlDocument.CreateElement("SpeechLine");
                    SpeechLine speechLine = npc.Speech[ii];

                    xmlSpeechLine.SetAttribute("text", speechLine.Text);

                    NpcActionContent speechLineOverrideContent = speechLine.OverrideContent;
                    if (speechLineOverrideContent != null) {
                        XmlElement xmlSpeechLineOverrideContent = xmlDocument.CreateElement("OverrideContent");

                        if (speechLineOverrideContent.AnimationName != null) {
                            xmlSpeechLineOverrideContent.SetAttribute("animationName", speechLineOverrideContent.AnimationName);
                        }
                        if (speechLineOverrideContent.SoundName != null) {
                            xmlSpeechLineOverrideContent.SetAttribute("soundName", speechLineOverrideContent.SoundName);
                        }

                        xmlSpeechLine.AppendChild(xmlSpeechLineOverrideContent);
                    }

                    xmlSpeech.AppendChild(xmlSpeechLine);
                }
                xmlNPC.AppendChild(xmlSpeech);

                XmlElement xmlActions = xmlDocument.CreateElement("Actions");
                foreach (NpcAction actionKey in npc.Actions.Keys) {
                    XmlElement xmlAction = xmlDocument.CreateElement("Action");
                    xmlAction.SetAttribute("key", actionKey.ToString());

                    XmlElement xmlActionContent = xmlDocument.CreateElement("NpcActionContent");
                    NpcActionContent actionContent = npc.Actions[actionKey];

                    if (actionContent.AnimationName != null) {
                        xmlActionContent.SetAttribute("animationName", actionContent.AnimationName);
                    }
                    if (actionContent.SoundName != null) {
                        xmlActionContent.SetAttribute("soundName", actionContent.SoundName);
                    }

                    xmlAction.AppendChild(xmlActionContent);
                    xmlActions.AppendChild(xmlAction);
                }
                xmlNPC.AppendChild(xmlActions);

                xmlNPCEntry.AppendChild(xmlNPC);
                xmlNPCs.AppendChild(xmlNPCEntry);
            }
            xmlLevel.AppendChild(xmlNPCs);

            XmlElement xmlPaths = xmlDocument.CreateElement("Paths");
            foreach (int key in levelData.Paths.Keys) {
                if (key < 0) {
                    continue;
                }
                XmlElement xmlPathEntry = xmlDocument.CreateElement("Entry");
                xmlPathEntry.SetAttribute("key", key.ToString());
                XmlElement xmlPath = xmlDocument.CreateElement("MovementPath");

                MovementPath path = levelData.Paths[key];
                xmlPath.SetAttribute("needsTrigger", path.NeedsTrigger.ToString());
                xmlPath.SetAttribute("isSpline", path.IsSpline.ToString());
                xmlPath.SetAttribute("offsetSeconds", path.OffsetSeconds.ToString());
                xmlPath.SetAttribute("saveTrigger", path.SaveTrigger.ToString());
                xmlPath.SetAttribute("endBehavior", path.EndBehavior.ToString());

                XmlElement xmlSegments = xmlDocument.CreateElement("Segments");
                for (int ii = 0; ii < path.Segments.Count; ii++) {
                    XmlElement xmlSegment = xmlDocument.CreateElement("PathSegment");
                    PathSegment segment = path.Segments[ii];

                    xmlSegment.SetAttribute("acceleration", segment.Acceleration.ToString());
                    xmlSegment.SetAttribute("deceleration", segment.Deceleration.ToString());
                    xmlSegment.SetAttribute("jitterFactor", segment.JitterFactor.ToString());
                    xmlSegment.SetAttribute("duration", segment.Duration.ToString());
                    xmlSegment.SetAttribute("waitTimeOnStart", segment.WaitTimeOnStart.ToString());
                    xmlSegment.SetAttribute("waitTimeOnFinish", segment.WaitTimeOnFinish.ToString());

                    XmlElement xmlSegmentDestination = xmlDocument.CreateElement("Destination");
                    XmlElement xmlSegmentDestinationV3 = xmlDocument.CreateElement("Vector3");
                    xmlSegmentDestinationV3.SetAttribute("x", segment.Destination.X.ToString());
                    xmlSegmentDestinationV3.SetAttribute("y", segment.Destination.Y.ToString());
                    xmlSegmentDestinationV3.SetAttribute("z", segment.Destination.Z.ToString());
                    xmlSegmentDestination.AppendChild(xmlSegmentDestinationV3);
                    xmlSegment.AppendChild(xmlSegmentDestination);

                    XmlElement xmlSegmentOrientation = xmlDocument.CreateElement("Orientation");
                    XmlElement xmlSegmentOrientationQ = xmlDocument.CreateElement("Quaternion");
                    xmlSegmentOrientationQ.SetAttribute("x", segment.Orientation.X.ToString());
                    xmlSegmentOrientationQ.SetAttribute("y", segment.Orientation.Y.ToString());
                    xmlSegmentOrientationQ.SetAttribute("z", segment.Orientation.Z.ToString());
                    xmlSegmentOrientationQ.SetAttribute("w", segment.Orientation.W.ToString());
                    xmlSegmentOrientation.AppendChild(xmlSegmentOrientationQ);
                    xmlSegment.AppendChild(xmlSegmentOrientation);

                    XmlElement xmlCustomData = xmlDocument.CreateElement("CustomData");
                    XmlElement xmlSegmentData = xmlDocument.CreateElement("CameraNodeData");

                    CameraNodeData segmentData = (CameraNodeData) segment.CustomData;
                    xmlSegmentData.SetAttribute("perspective", segmentData.Perspective.ToString());
                    xmlSegmentData.SetAttribute("pixelsPerTrixel", segmentData.PixelsPerTrixel.ToString());
                    xmlSegmentData.SetAttribute("soundName", segmentData.SoundName);

                    xmlCustomData.AppendChild(xmlSegmentData);
                    xmlSegment.AppendChild(xmlCustomData);

                    xmlSegments.AppendChild(xmlSegment);
                }
                xmlPath.AppendChild(xmlSegments);

                xmlPathEntry.AppendChild(xmlPath);
                xmlPaths.AppendChild(xmlPathEntry);
            }
            xmlLevel.AppendChild(xmlPaths);

            XmlElement xmlMutedLoops = xmlDocument.CreateElement("MutedLoops");
            for (int i = 0; i < levelData.MutedLoops.Count; i++) {
                XmlElement xmlMutedLoop = xmlDocument.CreateElement("Entry");
                xmlMutedLoop.InnerText = levelData.MutedLoops[i];
                xmlMutedLoops.AppendChild(xmlMutedLoop);
            }
            xmlLevel.AppendChild(xmlMutedLoops);

            XmlElement xmlAmbienceTracks = xmlDocument.CreateElement("AmbienceTracks");
            for (int i = 0; i < levelData.AmbienceTracks.Count; i++) {
                XmlElement xmlAmbienceTrack = xmlDocument.CreateElement("AmbienceTrack");
                AmbienceTrack ambienceTrack = levelData.AmbienceTracks[i];

                xmlAmbienceTrack.SetAttribute("dawn", ambienceTrack.Dawn.ToString());
                xmlAmbienceTrack.SetAttribute("day", ambienceTrack.Day.ToString());
                xmlAmbienceTrack.SetAttribute("dusk", ambienceTrack.Dusk.ToString());
                xmlAmbienceTrack.SetAttribute("night", ambienceTrack.Night.ToString());
                xmlAmbienceTrack.SetAttribute("name", ambienceTrack.Name);

                xmlAmbienceTracks.AppendChild(xmlAmbienceTrack);
            }
            xmlLevel.AppendChild(xmlAmbienceTracks);

            //Write to file
            xmlDocument.AppendChild(xmlLevel);
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

