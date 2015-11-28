using FezGame.Mod;
using FezGame.Components;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Editor {
    public class FezEditor : FezModule {

        public override string Name { get { return "FEZMod.FezEditor"; } }
        public override string Author { get { return "AngelDE98 & JAFM contributors"; } }
        public override string Version { get { return FEZMod.Version; } }

        public bool InEditor = false;

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
                    FEZMod.GetComponentsAsServices = true;
                    FEZMod.HandleComponents = true;
                    InEditor = true;
                }
            }
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

