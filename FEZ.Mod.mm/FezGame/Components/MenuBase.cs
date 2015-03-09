using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MonoMod;

namespace FezGame.Components {
    [MonoModIgnore]
    public class MenuBase {

        //stub clone of disassembled code - use with care

        public static readonly Action SliderAction;

        protected SpriteBatch SpriteBatch;

        protected SoundEffect sDisappear;

        //protected SelectorPhase selectorPhase;

        protected float sinceSelectorPhaseStarted;

        protected RenderTarget2D CurrentMenuLevelTexture;

        protected RenderTarget2D NextMenuLevelTexture;

        protected Mesh MenuLevelOverlay;

        protected SoundEffect sAppear;

        protected SoundEffect sReturnLevel;

        protected SoundEffect sScreenNarrowen;

        protected SoundEffect sScreenWiden;

        protected SoundEffect sSliderValueDecrease;

        protected SoundEffect sSliderValueIncrease;

        protected SoundEffect sStartGame;

        protected int currentResolution;

        protected Rectangle? BButtonRect;

        protected Rectangle? XButtonRect;

        private Texture2D CanClickCursor;

        private Texture2D PointerCursor;

        private Texture2D ClickedCursor;

        protected bool isDisposed;

        protected Rectangle? AButtonRect;

        protected bool isFullscreen;

        public bool EndGameMenu;

        protected bool StartedNewGame;

        public bool CursorSelectable;

        public bool CursorClicking;

        protected float SinceMouseMoved;

        protected SoundEffect sExitGame;

        protected MenuLevel AudioSettingsMenu;

        protected MenuLevel VideoSettingsMenu;

        //protected LeaderboardsMenuLevel LeaderboardsMenu;

        //protected ControlsMenuLevel ControlsMenu;

        //public CreditsMenuLevel CreditsMenu;

        protected List<MenuLevel> MenuLevels;

        protected MenuLevel GameSettingsMenu;

        protected MenuLevel CurrentMenuLevel;

        protected MenuLevel MenuRoot;

        protected MenuLevel UnlockNeedsLIVEMenu;

        protected MenuLevel HelpOptionsMenu;

        protected MenuLevel StartNewGameMenu;

        protected MenuLevel ExitToArcadeMenu;

        protected MenuItem StereoMenuItem;

        protected Mesh Mask;

        protected SoundEffect sAdvanceLevel;

        protected SoundEffect sCancel;

        protected SoundEffect sConfirm;

        protected SoundEffect sCursorUp;

        protected SoundEffect sCursorDown;

        protected Mesh Frame;

        protected MenuItem VibrationMenuItem;

        //protected SaveManagementLevel SaveManagementMenu;

        protected TimeSpan sliderDownLeft;

        public MenuLevel nextMenuLevel;

        protected MenuLevel lastMenuLevel;

        protected GlyphTextRenderer tr;

        protected Mesh Selector;

        public IContentManagerProvider CMProvider { protected get; set; }

        public IFontManager Fonts { protected get; set; }

        public IGameStateManager GameState { protected get; set; }

        public IInputManager InputManager { protected get; set; }

        public IKeyboardStateManager KeyboardState { protected get; set; }

        public IMouseStateManager MouseState { protected get; set; }

        public ISoundManager SoundManager { protected get; set; }

        public ITargetRenderingManager TargetRenderer { protected get; set; }

        protected MenuBase(Game game) {
        }

        protected virtual bool AllowDismiss() {
            return false;
        }

        protected virtual bool AlwaysShowBackButton() {
            return false;
        }

        private void ApplyVideo() {
        }

        public bool ChangeMenuLevel(MenuLevel next, bool silent = false) {
            return false;
        }

        protected virtual void ContinueGame() {
        }

        private void DestroyMenu() {
        }

        protected void Dispose(bool disposing) {
        }

        public void Draw(GameTime gameTime) {
        }

        private void DrawButtons() {
        }

        private void DrawLevel(MenuLevel level, bool toTexture) {
        }

        private void DynamicUpgrade() {
        }

        public void Initialize() {
        }

        protected void LoadContent() {
        }

        protected virtual void PostInitialize() {
        }

        private void RenderToTexture() {
        }

        private void Rescale() {
        }

        protected virtual void ResumeGame() {
        }

        protected virtual void ReturnToArcade() {
        }

        private void ReturnToAudioDefault() {
        }

        private void ReturnToGameDefault() {
        }

        private void ReturnToVideoDefault() {
        }

        private void Select(MenuLevel activeLevel) {
        }

        private void ShowAchievements() {
        }

        protected virtual void StartNewGame() {
        }

        private void ToggleStereo() {
        }

        private void ToggleVibration() {
        }

        private void UnlockFullGame() {
        }

        public void Update(GameTime gameTime) {
        }

        protected virtual bool UpdateEarlyOut() {
            return false;
        }

        private void UpdateSelector(float elapsedSeconds) {
        }

        private void UpOneLevel(MenuLevel activeLevel) {
        }

    }
}
