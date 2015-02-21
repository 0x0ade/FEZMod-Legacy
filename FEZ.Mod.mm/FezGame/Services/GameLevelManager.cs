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

namespace FezGame.Services {
	public class GameLevelManager : LevelManager {

        public Level oldLevel;

        private IGameStateManager orig_get_GameState() {
            return null;
        }

        private IGameStateManager get_GameState() {
            return orig_get_GameState();
        }

        public void orig_Load(string levelName) {
        }

        public void Load(string levelName) {
            if (MemoryContentManager.AssetExists("Levels/"+levelName)) {
                orig_Load(levelName);
                return;
            }
			string filePath = ("Resources\\levels\\"+(levelName.ToLower())).Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString())+".xml";
            FileInfo file = new FileInfo(filePath);
            if (!file.Exists) {
                orig_Load(levelName);
                return;
            }

            ModLogger.Log("JAFM.LevelMod", "Loading level from XML: "+levelName);

            ClearArtSatellites();

            oldLevel = levelData;
            levelData = new Level();

            Module moduleFezEngine = levelData.GetType().Module;//expecting FezEngine.Structure.Level

			FileStream fis = new FileStream(file.FullName, FileMode.Open);
            XmlReader xmlReader = XmlReader.Create(fis);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(xmlReader);
            xmlReader.Close();
            fis.Close();
            XmlElement xmlLevel = xmlDocument["Level"];
            XmlAttributeCollection xmlLevelAttribs = xmlLevel.Attributes;

            //Level attributes
            MethodInfo[] levelMethods = levelData.GetType().GetMethods();
            for (int i = 0; i < levelMethods.Length; i++) {
                MethodInfo method = levelMethods[i];
                string methodLowerCase = method.Name.ToLower();
                if (!methodLowerCase.StartsWith("set_")) {
                    continue;
                }

                XmlAttribute xmlLevelAttrib = null;
                for (int ii = 0; ii < xmlLevelAttribs.Count; ii++) {
                    XmlAttribute xmlLevelAttribI = xmlLevelAttribs[ii];
                    string xmlAttribILowerCase = xmlLevelAttribI.Name.ToLower();
                    if (methodLowerCase != "set_"+xmlAttribILowerCase) {
                        continue;
                    }
                    xmlLevelAttrib = xmlLevelAttribI;
                    break;
                }
                if (xmlLevelAttrib == null) {
                    continue;
                }
                string value = xmlLevelAttrib.Value;
                if (value == null) {
                    continue;
                }

                ParameterInfo[] methodParams = method.GetParameters();
                if (methodParams == null) {
                    continue;
                }
                ParameterInfo param = methodParams[0];
                Type paramType = param.ParameterType;
                if (paramType.FullName == "System.String") {
                    method.Invoke(levelData, new Object[] {value});
                } else {
                    if (Type.GetType("System.ValueType").IsAssignableFrom(paramType) || paramType.IsSubclassOf(Type.GetType("System.ValueType"))) {
                        MethodInfo parse = paramType.GetMethod("Parse", new Type[]{Type.GetType("System.String")});
                        if (parse != null) {
                            method.Invoke(levelData, new Object[]{parse.Invoke(null, new Object[]{value})});
                        }
                    } else if (Type.GetType("System.Enum").IsAssignableFrom(paramType) || paramType.IsSubclassOf(Type.GetType("System.Enum"))) {
                        MethodInfo parse = paramType.GetMethod("Parse", new Type[]{Type.GetType("System.Type"), Type.GetType("System.String")});
                        if (parse != null) {
                            method.Invoke(levelData, new Object[]{parse.Invoke(null, new Object[]{paramType, value})});
                        }
                    }
                }
            }

            ContentManager cm = get_CMProvider().GetForLevel(levelData.Name);
            //levelData.Name(levelName);

            //Load / prepare content (especially TrileSet)

            levelData.Sky = cm.Load<Sky>("Skies/" + levelData.SkyName);
            if (levelData.TrileSetName != null) {
                levelData.TrileSet = cm.Load<TrileSet>("Trile Sets/" + levelData.TrileSetName);
            }
            if (levelData.SongName != null) {
                levelData.Song = cm.Load<TrackedSong>("Music/" + levelData.SongName);
                levelData.Song.Initialize();
            }

            //Actual level content
            levelData.Size = (Vector3) XmlLevelHelper.Parse(xmlLevel["Size"]);

            XmlElement xmlSpawn = xmlLevel["StartingPosition"]["TrileFace"];
            levelData.StartingPosition = new TrileFace();
            levelData.StartingPosition.Face = (FaceOrientation) Enum.Parse(moduleFezEngine.GetType("FezEngine.FaceOrientation"), xmlSpawn.GetAttribute("face"));
            levelData.StartingPosition.Id = (TrileEmplacement) XmlLevelHelper.Parse(xmlSpawn["TrileId"]);

            XmlElement xmlVolumes = xmlLevel["Volumes"];
            levelData.Volumes = new Dictionary<int, Volume>();
            for (int i = 0; i < xmlVolumes.ChildNodes.Count; i++) {
                XmlElement xmlVolume = (XmlElement) xmlVolumes.ChildNodes.Item(i);
                int key = int.Parse(xmlVolume.GetAttribute("key"));
                xmlVolume = xmlVolume["Volume"];
                Volume volume = new Volume();

                //Thanks, IKVM!
                //warning IKVMC0100: Class "cli.System.Collections.Generic.HashSet$$00601_$$$_Lcli__FezEngine__FaceOrientation_$$$$_" not found
                HashSet<FaceOrientation> orientations = new HashSet<FaceOrientation>();
                XmlElement xmlOrientations = xmlVolume["Orientations"];
                for (int ii = 0; ii < xmlOrientations.ChildNodes.Count; ii++) {
                    XmlElement xmlOrientation = (XmlElement) xmlOrientations.ChildNodes.Item(ii);
                    orientations.Add((FaceOrientation)Enum.Parse(moduleFezEngine.GetType("FezEngine.FaceOrientation"), xmlOrientation.InnerText));
                }
                volume.Orientations = orientations;

                volume.From = (Vector3) XmlLevelHelper.Parse(xmlVolume["From"]);

                volume.To = (Vector3) XmlLevelHelper.Parse(xmlVolume["To"]);

                levelData.Volumes[key] = volume;
            }

            XmlElement xmlScripts = xmlLevel["Scripts"];
            levelData.Scripts = new Dictionary<int, Script>();
            for (int i = 0; i < xmlScripts.ChildNodes.Count; i++) {
                XmlElement xmlScript = (XmlElement) xmlScripts.ChildNodes.Item(i);
                int key = int.Parse(xmlScript.GetAttribute("key"));
                xmlScript = xmlScript["Script"];
                Script script = new Script();

                script.Name = xmlScript.GetAttribute("name");
                script.OneTime = bool.Parse(xmlScript.GetAttribute("oneTime"));
                script.Triggerless = bool.Parse(xmlScript.GetAttribute("triggerless"));
                script.IgnoreEndTriggers = bool.Parse(xmlScript.GetAttribute("ignoreEndTriggers"));
                script.LevelWideOneTime = bool.Parse(xmlScript.GetAttribute("levelWideOneTime"));
                script.Disabled = bool.Parse(xmlScript.GetAttribute("disabled"));
                script.IsWinCondition = bool.Parse(xmlScript.GetAttribute("isWinCondition"));

                List<ScriptTrigger> triggers = new List<ScriptTrigger>();
                XmlElement xmlTriggers = xmlScript["Triggers"];
                for (int ii = 0; ii < xmlTriggers.ChildNodes.Count; ii++) {
                    XmlElement xmlTrigger = (XmlElement) xmlTriggers.ChildNodes.Item(ii);
                    ScriptTrigger trigger = new ScriptTrigger();

                    trigger.Event = xmlTrigger.GetAttribute("event");

                    XmlElement xmlEntity = xmlTrigger["Entity"];
                    Entity entity = new Entity();
                    entity.Type = xmlEntity.GetAttribute("entityType");
                    string entityID = xmlEntity.GetAttribute("identifier");
                    if (!string.IsNullOrEmpty(entityID)) {
                        entity.Identifier = int.Parse(entityID);
                    } else {
                        entity.Identifier = new int?();
                    }
                    trigger.Object = entity;

                    triggers.Add(trigger);
                }
                script.Triggers = triggers;

                List<ScriptAction> actions = new List<ScriptAction>();
                XmlElement xmlActions = xmlScript["Actions"];
                for (int ii = 0; ii < xmlActions.ChildNodes.Count; ii++) {
                    XmlElement xmlAction = (XmlElement) xmlActions.ChildNodes.Item(ii);
                    ScriptAction action = new ScriptAction();

                    action.Operation = xmlAction.GetAttribute("operation");
                    action.Killswitch = bool.Parse(xmlAction.GetAttribute("killswitch"));
                    action.Blocking = bool.Parse(xmlAction.GetAttribute("blocking"));

                    XmlElement xmlEntity = xmlAction["Entity"];
                    Entity entity = new Entity();
                    entity.Type = xmlEntity.GetAttribute("entityType");
                    string entityID = xmlEntity.GetAttribute("identifier");
                    if (!string.IsNullOrEmpty(entityID)) {
                        entity.Identifier = int.Parse(entityID);
                    } else {
                        entity.Identifier = new int?();
                    }
                    action.Object = entity;

                    XmlElement xmlArguments = xmlAction["Arguments"];
                    String[] arguments = new String[xmlArguments.ChildNodes.Count];
                    for (int iii = 0; iii < arguments.Length; iii++) {
                        arguments[iii] = xmlArguments.ChildNodes.Item(iii).InnerText;
                    }
                    action.Arguments = arguments;

                    action.OnDeserialization();

                    actions.Add(action);
                }
                script.Actions = actions;

                levelData.Scripts[key] = script;
            }

            XmlElement xmlTriles = xmlLevel["Triles"];
            levelData.Triles = new Dictionary<TrileEmplacement, TrileInstance>();
            for (int i = 0; i < xmlTriles.ChildNodes.Count; i++) {
                XmlElement xmlTrile = (XmlElement) xmlTriles.ChildNodes.Item(i);

                TrileEmplacement trileEmplacement = (TrileEmplacement) XmlLevelHelper.Parse(xmlTrile);
                xmlTrile = xmlTrile["TrileInstance"];

                TrileInstance trile = new TrileInstance(trileEmplacement, int.Parse(xmlTrile.GetAttribute("trileId")));

                trile.Position = (Vector3) XmlLevelHelper.Parse(xmlTrile["Position"]);

                trile.SetPhiLight(byte.Parse(xmlTrile.GetAttribute("orientation")));

                levelData.Triles[trileEmplacement] = trile;
            }

            XmlElement xmlAOs = xmlLevel["ArtObjects"];
            levelData.ArtObjects = new Dictionary<int, ArtObjectInstance>();
            for (int i = 0; i < xmlAOs.ChildNodes.Count; i++) {
                XmlElement xmlAO = (XmlElement) xmlAOs.ChildNodes.Item(i);
                int key = int.Parse(xmlAO.GetAttribute("key"));
                xmlAO = xmlAO["ArtObjectInstance"];

                ArtObjectInstance ao = new ArtObjectInstance();

                ao.ArtObjectName = xmlAO.GetAttribute("name");
                ao.Position = (Vector3) XmlLevelHelper.Parse(xmlAO["Position"]);
                ao.Rotation = (Quaternion) XmlLevelHelper.Parse(xmlAO["Rotation"]);
                ao.Scale = (Vector3) XmlLevelHelper.Parse(xmlAO["Scale"]);

                XmlElement xmlAOSettings = xmlAO["ArtObjectActorSettings"];
                ArtObjectActorSettings settings = new ArtObjectActorSettings();

                settings.Inactive = bool.Parse(xmlAOSettings.GetAttribute("inactive"));
                settings.SpinEvery = float.Parse(xmlAOSettings.GetAttribute("spinEvery"));
                settings.SpinOffset = float.Parse(xmlAOSettings.GetAttribute("spinOffset"));
                settings.OffCenter = bool.Parse(xmlAOSettings.GetAttribute("offCenter"));
                settings.TimeswitchWindBackSpeed = float.Parse(xmlAOSettings.GetAttribute("timeswitchWindBackSpeed"));
                settings.ContainedTrile = (ActorType) Enum.Parse(moduleFezEngine.GetType("FezEngine.Structure.ActorType"), xmlAOSettings.GetAttribute("containedTrile"));
                string attachedGroup = xmlAOSettings.GetAttribute("attachedGroup");
                if (!string.IsNullOrEmpty(attachedGroup)) {
                    settings.AttachedGroup = int.Parse(attachedGroup);
                } else {
                    settings.AttachedGroup = new int?();
                }
                settings.SpinView = (Viewpoint) Enum.Parse(moduleFezEngine.GetType("FezEngine.Viewpoint"), xmlAOSettings.GetAttribute("spinView"));
                settings.RotationCenter = (Vector3) XmlLevelHelper.Parse(xmlAOSettings["RotationCenter"]);

                ao.ActorSettings = settings;

                ao.ArtObject = cm.Load<ArtObject>("Art Objects/"+ao.ArtObjectName);

                levelData.ArtObjects[key] = ao;
            }

            XmlElement xmlPlanes = xmlLevel["BackgroundPlanes"];
            levelData.BackgroundPlanes = new Dictionary<int, BackgroundPlane>();
            for (int i = 0; i < xmlPlanes.ChildNodes.Count; i++) {
                XmlElement xmlPlane = (XmlElement) xmlPlanes.ChildNodes.Item(i);
                int key = int.Parse(xmlPlane.GetAttribute("key"));
                xmlPlane = xmlPlane["BackgroundPlane"];

                BackgroundPlane plane = new BackgroundPlane();

                plane.TextureName = xmlPlane.GetAttribute("textureName");
                plane.LightMap = bool.Parse(xmlPlane.GetAttribute("lightMap"));
                plane.AllowOverbrightness = bool.Parse(xmlPlane.GetAttribute("allowOverbrightness"));
                plane.Animated = bool.Parse(xmlPlane.GetAttribute("animated"));
                plane.Doublesided = bool.Parse(xmlPlane.GetAttribute("doubleSided"));
                plane.Opacity = float.Parse(xmlPlane.GetAttribute("opacity"));
                plane.Billboard = bool.Parse(xmlPlane.GetAttribute("billboard"));
                plane.SyncWithSamples = bool.Parse(xmlPlane.GetAttribute("syncWithSamples"));
                plane.Crosshatch = bool.Parse(xmlPlane.GetAttribute("crosshatch"));
                //plane.Unknown = bool.Parse(xmlPlane.GetAttribute("unknown"));//Troll reference XML is troll.
                plane.AlwaysOnTop = bool.Parse(xmlPlane.GetAttribute("alwaysOnTop"));
                plane.Fullbright = bool.Parse(xmlPlane.GetAttribute("fullbright"));
                plane.PixelatedLightmap = bool.Parse(xmlPlane.GetAttribute("pixelatedLightmap"));
                plane.XTextureRepeat = bool.Parse(xmlPlane.GetAttribute("xTextureRepeat"));
                plane.YTextureRepeat = bool.Parse(xmlPlane.GetAttribute("yTextureRepeat"));
                plane.ClampTexture = bool.Parse(xmlPlane.GetAttribute("clampTexture"));
                plane.ParallaxFactor = float.Parse(xmlPlane.GetAttribute("parallaxFactor"));
                Color filter = new Color();
                filter.PackedValue = Convert.ToUInt32(xmlPlane.GetAttribute("filter").Substring(1), 16);
                plane.Filter = filter;
                plane.ActorType = (ActorType) Enum.Parse(moduleFezEngine.GetType("FezEngine.Structure.ActorType"), xmlPlane.GetAttribute("actorType"));
                plane.Position = (Vector3) XmlLevelHelper.Parse(xmlPlane["Position"]);
                plane.Rotation = (Quaternion) XmlLevelHelper.Parse(xmlPlane["Rotation"]);
                plane.Scale = (Vector3) XmlLevelHelper.Parse(xmlPlane["Scale"]);

                levelData.BackgroundPlanes[key] = plane;
            }

            XmlElement xmlGroups = xmlLevel["Groups"];
            levelData.Groups = new Dictionary<int, TrileGroup>();
            for (int i = 0; i < xmlGroups.ChildNodes.Count; i++) {
                XmlElement xmlGroup = (XmlElement) xmlGroups.ChildNodes.Item(i);
                int key = int.Parse(xmlGroup.GetAttribute("key"));
                xmlGroup = xmlGroup["TrileGroup"];

                TrileGroup trileGroup = new TrileGroup();

                trileGroup.Heavy = bool.Parse(xmlGroup.GetAttribute("heavy"));
                trileGroup.GeyserOffset = float.Parse(xmlGroup.GetAttribute("geyserOffset"));
                trileGroup.GeyserPauseFor = float.Parse(xmlGroup.GetAttribute("geyserPauseFor"));
                trileGroup.GeyserLiftFor = float.Parse(xmlGroup.GetAttribute("geyserLiftFor"));
                trileGroup.GeyserApexHeight = float.Parse(xmlGroup.GetAttribute("geyserApexHeight"));
                trileGroup.SpinClockwise = bool.Parse(xmlGroup.GetAttribute("spinClockwise"));
                trileGroup.SpinFrequency = float.Parse(xmlGroup.GetAttribute("spinFrequency"));
                trileGroup.SpinNeedsTriggering = bool.Parse(xmlGroup.GetAttribute("spinNeedsTriggering"));
                trileGroup.Spin180Degrees = bool.Parse(xmlGroup.GetAttribute("spin180Degrees"));
                trileGroup.FallOnRotate = bool.Parse(xmlGroup.GetAttribute("fallOnRotate"));
                trileGroup.SpinOffset = float.Parse(xmlGroup.GetAttribute("spinOffset"));
                trileGroup.ActorType = (ActorType) Enum.Parse(moduleFezEngine.GetType("FezEngine.Structure.ActorType"), xmlGroup.GetAttribute("actorType"));

                trileGroup.Triles = new List<TrileInstance>();
                XmlElement xmlGroupTriles = xmlGroup["Triles"];
                for (int ii = 0; ii < xmlGroupTriles.ChildNodes.Count; ii++) {
                    XmlElement xmlGroupTrile = (XmlElement) xmlGroupTriles.ChildNodes.Item(ii);

                    TrileInstance groupTrile = new TrileInstance((Vector3) XmlLevelHelper.Parse(xmlGroupTrile["Position"]),
                        int.Parse(xmlGroupTrile.GetAttribute("trileId")));

                    groupTrile.SetPhiLight(byte.Parse(xmlGroupTrile.GetAttribute("orientation")));

                    trileGroup.Triles.Add(groupTrile);
                }

                trileGroup.SpinCenter = (Vector3) XmlLevelHelper.Parse(xmlGroup["SpinCenter"]);

                levelData.Groups[key] = trileGroup;
            }

            XmlElement xmlNPCs = xmlLevel["NonplayerCharacters"];
            levelData.NonPlayerCharacters = new Dictionary<int, NpcInstance>();
            for (int i = 0; i < xmlNPCs.ChildNodes.Count; i++) {
                XmlElement xmlNPC = (XmlElement) xmlNPCs.ChildNodes.Item(i);
                int key = int.Parse(xmlNPC.GetAttribute("key"));
                xmlNPC = xmlNPC["NpcInstance"];

                NpcInstance npc = new NpcInstance();
				npc.Name = xmlNPC.GetAttribute("name");
				npc.WalkSpeed = float.Parse(xmlNPC.GetAttribute("walkSpeed"));
                if (!string.IsNullOrEmpty(xmlNPC.GetAttribute("randomizeSpeed"))) {
                    //parsed via old versions of xnb_parse with the typo
                    npc.RandomizeSpeech = bool.Parse(xmlNPC.GetAttribute("randomizeSpeed"));
                    npc.SayFirstSpeechLineOnce = bool.Parse(xmlNPC.GetAttribute("sayFirstSpeedLineOnce"));
                } else {
                    //parsed via new versions of xnb_parse without the typo
                    npc.RandomizeSpeech = bool.Parse(xmlNPC.GetAttribute("randomizeSpeech"));
                    npc.SayFirstSpeechLineOnce = bool.Parse(xmlNPC.GetAttribute("sayFirstSpeechLineOnce"));
                }
				npc.AvoidsGomez = bool.Parse(xmlNPC.GetAttribute("avoidsGomez"));
				npc.ActorType = (ActorType) Enum.Parse(moduleFezEngine.GetType("FezEngine.Structure.ActorType"), xmlNPC.GetAttribute("actorType"));

                npc.Position = (Vector3) XmlLevelHelper.Parse(xmlNPC["Position"]);

                npc.DestinationOffset = (Vector3) XmlLevelHelper.Parse(xmlNPC["DestinationOffset"]);

                XmlElement xmlSpeech = xmlNPC["Speech"];
                List<SpeechLine> speech = new List<SpeechLine>();
                for (int ii = 0; ii < xmlSpeech.ChildNodes.Count; ii++) {
                    XmlElement xmlSpeechLine = (XmlElement) xmlSpeech.ChildNodes.Item(ii);
                    SpeechLine speechLine = new SpeechLine();

					speechLine.Text = xmlSpeechLine.GetAttribute("text");

                    XmlElement xmlSpeechLineOverrideContent = xmlSpeechLine["OverrideContent"];
                    if (xmlSpeechLineOverrideContent != null) {
                        NpcActionContent speechLineOverrideContent = new NpcActionContent();
                        if (!string.IsNullOrEmpty(xmlSpeechLineOverrideContent.GetAttribute("animationName"))) {
                            speechLineOverrideContent.AnimationName = xmlSpeechLineOverrideContent.GetAttribute("animationName");
                        }
                        if (!string.IsNullOrEmpty(xmlSpeechLineOverrideContent.GetAttribute("soundName"))) {
                            speechLineOverrideContent.SoundName = xmlSpeechLineOverrideContent.GetAttribute("soundName");
                        }
						speechLine.OverrideContent = speechLineOverrideContent;
                    }

                    speech.Add(speechLine);
                }
				npc.Speech = speech;

                XmlElement xmlActions = xmlNPC["Actions"];
                Dictionary<NpcAction, NpcActionContent> actions = new Dictionary<NpcAction, NpcActionContent>();
                for (int ii = 0; ii < xmlActions.ChildNodes.Count; ii++) {
                    XmlElement xmlAction = (XmlElement) xmlActions.ChildNodes.Item(ii);
                    NpcAction action = (NpcAction) Enum.Parse(moduleFezEngine.GetType("FezEngine.Structure.NpcAction"), xmlAction.GetAttribute("key"));

                    XmlElement xmlActionContent = xmlAction["NpcActionContent"];
                    NpcActionContent actionContent = new NpcActionContent();

                    if (!string.IsNullOrEmpty(xmlActionContent.GetAttribute("animationName"))) {
                        actionContent.AnimationName = xmlActionContent.GetAttribute("animationName");
                    }
                    if (!string.IsNullOrEmpty(xmlActionContent.GetAttribute("soundName"))) {
                        actionContent.SoundName = xmlActionContent.GetAttribute("soundName");
                    }

					actions[action] = actionContent;
                }
				npc.Actions = actions;

                levelData.NonPlayerCharacters[key] = npc;
            }

            XmlElement xmlPaths = xmlLevel["Paths"];
            levelData.Paths = new Dictionary<int, MovementPath>();
            for (int i = 0; i < xmlPaths.ChildNodes.Count; i++) {
                XmlElement xmlPath = (XmlElement) xmlPaths.ChildNodes.Item(i);
                int key = int.Parse(xmlPath.GetAttribute("key"));
                xmlPath = xmlPath["MovementPath"];

                MovementPath path = new MovementPath();
				path.NeedsTrigger = bool.Parse(xmlPath.GetAttribute("needsTrigger"));
				path.IsSpline = bool.Parse(xmlPath.GetAttribute("isSpline"));
				path.OffsetSeconds = float.Parse(xmlPath.GetAttribute("offsetSeconds"));
				path.SaveTrigger = bool.Parse(xmlPath.GetAttribute("saveTrigger"));
				path.EndBehavior = (PathEndBehavior) Enum.Parse(moduleFezEngine.GetType("FezEngine.Structure.PathEndBehavior"), xmlPath.GetAttribute("endBehavior"));

                XmlElement xmlSegments = xmlPath["Segments"];
                List<PathSegment> segments = new List<PathSegment>();
                for (int ii = 0; ii < xmlSegments.ChildNodes.Count; ii++) {
                    XmlElement xmlSegment = (XmlElement) xmlSegments.ChildNodes.Item(ii);
                    PathSegment segment = new PathSegment();

					segment.Acceleration = float.Parse(xmlSegment.GetAttribute("acceleration"));
					segment.Deceleration = float.Parse(xmlSegment.GetAttribute("deceleration"));
					segment.JitterFactor = float.Parse(xmlSegment.GetAttribute("jitterFactor"));
					segment.Duration = new TimeSpan(long.Parse(xmlSegment.GetAttribute("duration")));
					segment.WaitTimeOnStart = new TimeSpan(long.Parse(xmlSegment.GetAttribute("waitTimeOnStart")));
					segment.WaitTimeOnFinish = new TimeSpan(long.Parse(xmlSegment.GetAttribute("waitTimeOnFinish")));

                    segment.Destination = (Vector3) XmlLevelHelper.Parse(xmlSegment["Destination"]);

                    segment.Orientation = (Quaternion) XmlLevelHelper.Parse(xmlSegment["Orientation"]);

                    XmlElement xmlSegmentData = xmlSegment["CustomData"]["CameraNodeData"];
                    CameraNodeData segmentData = new CameraNodeData();

					segmentData.Perspective = bool.Parse(xmlSegmentData.GetAttribute("perspective"));
					segmentData.PixelsPerTrixel = int.Parse(xmlSegmentData.GetAttribute("pixelsPerTrixel"));
                    if (!string.IsNullOrEmpty(xmlSegmentData.GetAttribute("soundName"))) {
                        segmentData.SoundName = xmlSegmentData.GetAttribute("soundName");
                    }
					segment.CustomData = segmentData;

                    segments.Add(segment);
                }
                path.Segments = segments;

                levelData.Paths[key] = path;
            }

            XmlElement xmlMutedLoops = xmlLevel["MutedLoops"];
            levelData.MutedLoops = new List<string>();
            for (int i = 0; i < xmlMutedLoops.ChildNodes.Count; i++) {
                XmlElement xmlMutedLoop = (XmlElement) xmlMutedLoops.ChildNodes.Item(i);
                levelData.MutedLoops.Add(xmlMutedLoop.InnerText);
            }

            XmlElement xmlAmbienceTracks = xmlLevel["AmbienceTracks"];
            levelData.AmbienceTracks = new List<AmbienceTrack>();
            for (int i = 0; i < xmlAmbienceTracks.ChildNodes.Count; i++) {
                XmlElement xmlAmbienceTrack = (XmlElement) xmlAmbienceTracks.ChildNodes.Item(i);
                AmbienceTrack ambienceTrack = new AmbienceTrack();

				ambienceTrack.Dawn = bool.Parse(xmlAmbienceTrack.GetAttribute("dawn"));
				ambienceTrack.Day = bool.Parse(xmlAmbienceTrack.GetAttribute("day"));
				ambienceTrack.Dusk = bool.Parse(xmlAmbienceTrack.GetAttribute("dusk"));
				ambienceTrack.Night = bool.Parse(xmlAmbienceTrack.GetAttribute("night"));
				ambienceTrack.Name = xmlAmbienceTrack.GetAttribute("name");

                levelData.AmbienceTracks.Add(ambienceTrack);
            }

            levelData.OnDeserialization();

            //Do some save data stuff
            LevelSaveData save = get_GameState().SaveData.ThisLevel;
            if (save != null) {
                save.FirstVisit = false;
            }
        }

        public void Save(string levelName) {
            string filePath = ("Resources\\levels\\"+(levelName.ToLower())).Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString())+".xml";
            FileInfo file = new FileInfo(filePath);
            if (file.Exists) {
                return;
            }

            ModLogger.Log("JAFM.LevelMod", "Saving level to XML: "+levelName);

            Module moduleFezEngine = levelData.GetType().Module;//expecting FezEngine.Structure.Level

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
                    Type.GetType("System.ValueType").IsAssignableFrom(returnType) || returnType.IsSubclassOf(Type.GetType("System.ValueType")) ||
                    Type.GetType("System.Enum").IsAssignableFrom(returnType) || returnType.IsSubclassOf(Type.GetType("System.Enum")) ||
                    Type.GetType("System.String").IsAssignableFrom(returnType) || returnType.IsSubclassOf(Type.GetType("System.String"))
                )) {
                    continue;
                }

                string attributeName = methodLowerCase.Substring(4);
                Object value = method.Invoke(levelData, new Object[0]);
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
                xmlTrile.SetAttribute("orientation", ((int) ((trile.Data.PositionPhi.W / 1.570796f) + 2)).ToString());
                
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

                XmlElement xmlPlaneEntry = xmlDocument.CreateElement("Entry");
                xmlPlaneEntry.SetAttribute("key", key.ToString());
                XmlElement xmlPlane = xmlDocument.CreateElement("BackgroundPlane");

                BackgroundPlane plane = levelData.BackgroundPlanes[key];

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
        }

    }
}

