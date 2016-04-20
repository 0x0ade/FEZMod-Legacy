using Common;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame;
using FezGame.Services;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Globalization;
using System.Threading;
using MonoMod;
using FezGame.Mod;
using FezEngine.Mod;

namespace FezGame.Components {
    //TODO allow mods to patch intro without killing each other
    //FIXME MonoMod nested types in patch_ types
    public class Intro : DrawableGameComponent {
        
        [MonoModIgnore] private readonly Mesh TrixelPlanes = null;
        [MonoModIgnore] private Texture2D TrixelEngineText;
        [MonoModIgnore] private Texture2D TrapdoorLogo;
        [MonoModIgnore] private GlyphTextRenderer tr;
        [MonoModIgnore] private Mesh SaveIndicatorMesh;
        [MonoModIgnore] private Mesh TrialMesh;
        [MonoModIgnore] private FezLogo FezLogo;
        //[MonoModIgnore] private PolytronLogo PolytronLogo;
        //[MonoModIgnore] private IntroZoomIn IntroZoomIn;
        //[MonoModIgnore] private IntroPanDown IntroPanDown;
        //[MonoModIgnore] private MainMenu MainMenu;
        [MonoModIgnore] private SoundEffect sTitleBassHit;
        [MonoModIgnore] private SoundEffect sTrixelIn;
        [MonoModIgnore] private SoundEffect sTrixelOut;
        [MonoModIgnore] private SoundEffect sExitGame;
        [MonoModIgnore] private SoundEffect sConfirm;
        [MonoModIgnore] private SoundEffect sDrone;
        [MonoModIgnore] private SoundEffect sStarZoom;
        [MonoModIgnore] private SoundEmitter eDrone;
        [MonoModIgnore] private Intro.Phase phase;
        [MonoModIgnore] private Intro.Screen screen;
        [MonoModIgnore] private SpriteBatch spriteBatch;
        [MonoModIgnore] private TimeSpan phaseTime;
        [MonoModIgnore] private bool scheduledBackToGame;
        [MonoModIgnore] private bool ZoomToHouse;
        [MonoModIgnore] private static bool PreloadStarted;
        [MonoModIgnore] private static bool PreloadComplete;
        [MonoModIgnore] private static bool FirstBootComplete;
        [MonoModIgnore] private float opacity;
        [MonoModIgnore] private float promptOpacity;
        [MonoModIgnore] private static StarField Starfield;
        [MonoModIgnore] private static bool HasShownSaveIndicator;
        [MonoModIgnore] private static bool firstDrawDone;
        [MonoModIgnore] private float dotdotdot;
        [MonoModIgnore] private bool didPanDown;
        
        public static Intro Instance { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public bool Glitch { [MonoModIgnore] get; [MonoModIgnore] set; }
        public bool Fake { [MonoModIgnore] get; [MonoModIgnore] set; }
        public bool Sell { [MonoModIgnore] get; [MonoModIgnore] set; }
        public bool FadeBackToGame { [MonoModIgnore] get; [MonoModIgnore] set; }
        public bool Restarted { [MonoModIgnore] get; [MonoModIgnore] set; }
        public bool NewSaveSlot { [MonoModIgnore] get; [MonoModIgnore] set; }
        public string FakeLevel { [MonoModIgnore] get; [MonoModIgnore] set; }
        public bool FullLogos { [MonoModIgnore] get; [MonoModIgnore] set; }
        
        public ISoundManager SoundManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IPhysicsManager PhysicsManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        public ICollisionManager CollisionManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        public ITimeManager TimeManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IPlayerManager PlayerManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IDefaultCameraManager CameraManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        public ITargetRenderingManager TargetRenderer { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IGameLevelManager LevelManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IInputManager InputManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IGameStateManager GameState { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IThreadPool ThreadPool { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IFontManager Fonts { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IGameService GameService { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IContentManagerProvider CMProvider { [MonoModIgnore] get; [MonoModIgnore] set; }
        
        public Intro(Game game)
            : base(game) {
            //no-op
        }
        
        //Initialize gets called after LoadContent
        //public extern void orig_Initialize();
        /*public override void Initialize() {
            //orig_Initialize();
            //There is no original Initialize..?!
            base.Initialize();
            
            CustomIntroHelper.Create(this);
        }*/
        
        protected extern void orig_LoadContent();
        protected override void LoadContent() {
            orig_LoadContent();
            CustomIntroHelper.Create(this);
        }
        
        private extern void orig_ChangeScreen();
        private void ChangeScreen() {
            if (screen == Screen.Trapdoor) {
                screen = Screen.FEZMOD;
                return;
            }
            if (screen == Screen.FEZMOD) {
                if (CustomIntroHelper.Current == null) {
                    screen = Screen.Trapdoor;
                } else {
                    return;
                }
            }
            
            orig_ChangeScreen();
        }
        public void ForceChangeScreen() {
            orig_ChangeScreen();
        }
        
        private GameTime _gameTime;
        public extern void orig_Update(GameTime gameTime);
        public void Update(GameTime gameTime) {
            //We hook Update to get the gameTime for UpdateLogo.
            _gameTime = gameTime;
            
            orig_Update(gameTime);
        }
        
        private extern void orig_UpdateLogo();
        private void UpdateLogo() {
            if (screen != Screen.FEZMOD) {
                orig_UpdateLogo();
                return;
            }
            
            if (phase == Phase.FadeIn) {
                CustomIntroHelper.Reset();
            }
            if (phase == Phase.FadeIn || phase == Phase.FadeOut || CustomIntroHelper.Current == null) {
                ChangePhase();
            }
            
            if (CustomIntroHelper.Current != null) {
                CustomIntroHelper.Current.Update(_gameTime);
            }
        }
        
        public extern void orig_Draw(GameTime gameTime);
        public override void Draw(GameTime gameTime) {
            if (Fez.SkipLogos || screen != Screen.FEZMOD) {
                orig_Draw(gameTime);
                return;
            }
            
            if (CustomIntroHelper.Current != null) {
                CustomIntroHelper.Current.Draw(_gameTime);
            }
        }
        
        protected extern void orig_Dispose(bool disposing);
        protected override void Dispose(bool disposing) {
            orig_Dispose(disposing);
            
            CustomIntroHelper.Dispose();
        }
        
        private extern void orig_ChangePhase();
        private void ChangePhase() {
            if (screen != Screen.FEZMOD || (screen == Screen.FEZMOD && phase != Phase.Wait) || CustomIntroHelper.Current == null) {
                orig_ChangePhase();
                return;
            }
            
            if (CustomIntroHelper.Current != null) {
                CustomIntroHelper.Current.ChangePhase();
            }
        }
        
        [MonoModEnumReplace]
        private enum Screen {
            //Original enum values, must be the same to original to avoid an hardcoded / resolved value mismatch
            ESRB_PEGI,
            XBLA,
            MGS,
            WhiteScreen,
            Polytron,
            Trapdoor,
            TrixelEngine,
            SaveIndicator,
            Fez,
            SellScreen,
            Zoom,
            SignOutPrompt,
            SignInChooseDevice,
            MainMenu,
            Warp,
            //Custom values
            FEZMOD = 100
        }
        
        [MonoModIgnore]
        private enum Phase {
            FadeIn,
            Wait,
            FadeOut
        }
        
    }
}

