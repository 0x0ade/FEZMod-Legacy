using FezGame.Mod;
using FezEngine.Mod;
using FezGame.Components;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Editor {
    
    public class FezEditorSettings : FezModuleSettings {
        public Color DefaultForeground = Color.White;
        public Color DefaultBackground = new Color(0f, 0f, 0f, 0.75f);

        public bool TooltipArtObjectInfo = false;
        
        public int BackupHistory = 5;
    }
    
    public class FezEditor : FezModule {

        public override string Name { get { return "FEZMod.FezEditor"; } }
        public override string Author { get { return "AngelDE98 & JAFM contributors"; } }
        public override string Version { get { return FEZMod.Version; } }
        
        public static FezEditorSettings Settings;

        public static bool InEditor = false;

        public FezEditor() {
        }

        public override void ParseArgs(string[] args) {
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-e" || args[i] == "--editor") {
                    if (i+1 < args.Length && !args[i+1].StartsWith("-")) {
                        ModLogger.Log("FEZMod.FezEditor", "Found -e / --editor: "+args[i+1]);
                        Fez.ForcedLevelName = args[i+1];
                    } else {
                        ModLogger.Log("FEZMod.FezEditor", "Found -e / --editor");
                    }
                    //Fez.SkipLogos = true;
                    Fez.SkipIntro = true;
                    FEZMod.EnableDebugControls = true;
                    FEZModEngine.GetComponentsAsServices = true;
                    FEZModEngine.HandleComponents = true;
                    InEditor = true;
                }
            }
        }
        
        public override void Initialize() {
            Settings = FezModuleSettings.Load<FezEditorSettings>("FEZMod.Editor.Settings.sdl", new FezEditorSettings());
        }

        public override void LoadComponents(Fez game) {
            if (InEditor) {
                ServiceHelper.AddComponent(new LevelEditor(ServiceHelper.Game));
            }
        }

        public override void Preload() {
            if (LevelEditor.Instance != null) {
                LevelEditor.Instance.Preload();
            }
        }

    }
    
}

