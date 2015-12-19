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
        
        private readonly Mesh TrixelPlanes = null;
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
        
        private Texture2D adeLogoTL;
        private Texture2D adeLogoDR;
        private float adeOffset = 0f;
        
        private Texture2D segaLogo;
        
        protected extern void orig_LoadContent();
        protected override void LoadContent() {
            orig_LoadContent();
            
            if (FEZModEngine.Settings.ShowADELogo) {
                ContentManager cm = CMProvider.Get(CM.Intro);
                adeLogoTL = cm.Load<Texture2D>("other textures/splash/ade_tl");
                adeLogoDR = cm.Load<Texture2D>("other textures/splash/ade_dr");
            }
            
        }
        
        private extern void orig_ChangeScreen();
        private void ChangeScreen() {
            if (!FEZModEngine.Settings.ShowADELogo) {
                orig_ChangeScreen();
                return;
            }
            
            if (screen == Screen.Trapdoor) {
                screen = Screen.ADE;
                return;
            }
            if (screen == Screen.ADE) {
                screen = Screen.Trapdoor;
                orig_ChangeScreen();
                return;
            }
            
            orig_ChangeScreen();
        }
        
        private extern void orig_UpdateLogo();
        private void UpdateLogo() {
            if (screen != Screen.ADE || !FEZModEngine.Settings.ShowADELogo) {
                orig_UpdateLogo();
                return;
            }
            
            double time = phaseTime.TotalSeconds;
            if (phase == Phase.Wait) {
                ChangePhase();
                time = 0.0;
            }
            switch (phase) {
                case Phase.FadeIn:
                    if (time >= 1.0) {
                        ChangePhase();
                        break;
                    }
                    opacity = Easing.EaseIn(FezMath.Saturate(time * 8.0), EasingType.Quadratic);
                    //adeOffset = 1f + (float) Math.Cos(Math.PI * (time / 1.0)) + 1f;
                    adeOffset = 1f + Easing.EaseIn(1f - time, EasingType.Quadratic);
                    break;
                case Phase.FadeOut:
                    if (time >= 0.5) {
                        ChangePhase();
                        opacity = 0f;
                        break;
                    }
                    opacity = 1f - Easing.EaseOut(Math.Max((time - 0.25) / 0.25, 0.0), EasingType.Quadratic);
                    adeOffset = (float) Math.Cos(Math.PI * (time / 1.0));
                    break;
            }
            
        }
        
        public extern void orig_Draw(GameTime gameTime);
        public override void Draw(GameTime gameTime) {
            if (Fez.SkipLogos || screen != Screen.ADE || !FEZModEngine.Settings.ShowADELogo) {
                orig_Draw(gameTime);
                return;
            }
            
            GraphicsDevice.Clear(Color.White);
            
            Vector2 screenSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            Vector2 screenMid = FezMath.Round(screenSize / 2f);
            
            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);
            
            Vector2 offsTL = 0.25f * new Vector2(adeLogoTL.Width, -adeLogoTL.Height) * (1f - adeOffset);
            Vector2 offsDR = -offsTL;
            
            GraphicsDeviceExtensions.BeginPoint(spriteBatch);
            spriteBatch.Draw(
                adeLogoTL,
                screenMid - FezMath.Round(new Vector2(adeLogoTL.Width, adeLogoTL.Height) / 2f) + offsTL,
                new Color(1f, 1f, 1f, opacity)
            );
            spriteBatch.Draw(
                adeLogoDR,
                screenMid - FezMath.Round(new Vector2(adeLogoTL.Width, adeLogoDR.Height) / 2f) + offsDR,
                new Color(1f, 1f, 1f, opacity)
            );
            spriteBatch.End();
        }
        
        [MonoModIgnore]
        private extern void ChangePhase();
        
        [MonoModEnumReplace]
        private enum Screen {
            //Original enum values, must be the same to original due to possibly inlined / hardcoded / ?? values
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
            ADE = 100
        }
        
        [MonoModIgnore]
        private enum Phase {
            FadeIn,
            Wait,
            FadeOut
        }
        
    }
}