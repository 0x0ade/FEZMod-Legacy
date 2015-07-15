using System;
using Microsoft.Xna.Framework;
using System.IO;
using ContentSerialization.Attributes;
using System.Xml;
using FezGame.Mod;

namespace FezGame.Components {
    public class LevelEditorOptions {

        public static LevelEditorOptions Instance;

        public LevelEditorOptions() {
        }

        public static FileInfo FileDefault = new FileInfo("./FEZMod.Editor.Settings.xml");

        public Color DefaultForeground = Color.White;
        public Color DefaultBackground = new Color(0f, 0f, 0f, 0.75f);

        public bool TooltipArtObjectInfo = false;
        
        public int BackupHistory = 5;

        public static LevelEditorOptions Load(FileInfo file = null) {
            file = file ?? FileDefault;

            if (!file.Exists) {
                return new LevelEditorOptions();
            }

            XmlDocument xmlDocument = new XmlDocument();

            using (FileStream fs = new FileStream(file.FullName, FileMode.Open)) {
                XmlReader xmlReader = XmlReader.Create(fs);
                xmlDocument.Load(xmlReader);
                xmlReader.Close();
            }

            XmlElement xmlOptions = xmlDocument["LevelEditorOptions"];

            return (LevelEditorOptions) xmlOptions.Deserialize(null, null, true);
        }

        public void Save(FileInfo file = null) {
            Save(this);
        }

        public static void Save(LevelEditorOptions options = null, FileInfo file = null) {
            options = options ?? Instance;
            file = file ?? FileDefault;

            if (file.Exists) {
                file.Delete();
            }

            //TODO XmlHelper.Serialize required
        }

        public static void Initialize() {
            Instance = Load();
        }

    }
}

