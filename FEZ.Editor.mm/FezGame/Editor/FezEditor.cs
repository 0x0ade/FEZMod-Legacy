using FezGame.Mod;
using FezEngine.Mod;
using FezGame.Components;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using System;
using FezGame.Services;
using Microsoft.Xna.Framework.Input;

namespace FezGame.Editor {
    
    public class FezEditorSettings : FezModuleSettings {
        public Color DefaultForeground = Color.White;
        public Color DefaultBackground = new Color(0f, 0f, 0f, 0.75f);

        [Obsolete("Not implemented anymore - kept to keep settings intact")]
        public bool TooltipArtObjectInfo = false;
        
        public bool FogEnabled = false;
        
        public Keys KeyPerspective = Keys.P;
        public Keys KeyCamForwards = Keys.W;
        public Keys KeyCamLeft = Keys.A;
        public Keys KeyCamBack = Keys.S;
        public Keys KeyCamRight = Keys.D;
        public float FreeCamSpeed = 0.7f;
        
        public Keys KeyDelete = Keys.Delete;
        
        //LCTRL +
        public Keys KeySave = Keys.S;
        public Keys KeyNew = Keys.N;
        public Keys KeyOpen = Keys.O;
        public Keys KeyCopy = Keys.C;
        public Keys KeyCut = Keys.X;
        
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
                    FEZModEngine.GetComponentsAsServices = true;
                    FEZModEngine.HandleComponents = true;
                    InEditor = true;
                }
            }
        }
        
        public override void PreInitialize() {
            patch_ServiceHelper.AddHooks[typeof(GameCameraManager)] = delegate(IGameComponent component, bool addServices) {
                if (component is GameComponent) {
                    ((GameComponent) component).Dispose();
                }
                
                ServiceHelper.AddComponent(new EditorCameraManager(ServiceHelper.Game), addServices);
                
                return false;
            };
        }
        
        public override void Initialize() {
            Settings = FezModuleSettings.Load<FezEditorSettings>("FEZMod.Editor.Settings.sdl", new FezEditorSettings());
        }

        public override void LoadComponents(Fez game) {
            if (InEditor) {
                ServiceHelper.AddComponent(new LevelEditor(ServiceHelper.Game));
            }
        }

    }
    
}

