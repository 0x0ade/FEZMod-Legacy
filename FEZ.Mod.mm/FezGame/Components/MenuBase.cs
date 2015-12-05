using Common;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MonoMod;
using FezEngine.Mod;
using FezGame.Mod;

namespace FezGame.Components {
    //internal in FEZ
	public class MenuBase : DrawableGameComponent {
        
        [MonoModIgnore]
		public static readonly Action SliderAction = delegate {
		};

        [MonoModIgnore] protected SpriteBatch SpriteBatch;
        [MonoModIgnore] protected GlyphTextRenderer tr;
        
        [MonoModIgnore] protected RenderTarget2D CurrentMenuLevelTexture;
        [MonoModIgnore] protected RenderTarget2D NextMenuLevelTexture;
        
        [MonoModIgnore] private Texture2D CanClickCursor;
        [MonoModIgnore] private Texture2D PointerCursor;
        [MonoModIgnore] private Texture2D ClickedCursor;
        
        [MonoModIgnore] protected Mesh MenuLevelOverlay;
        [MonoModIgnore] protected Mesh Mask;
        [MonoModIgnore] protected Mesh Frame;
        [MonoModIgnore] protected Mesh Selector;
        
        [MonoModIgnore] protected Rectangle? AButtonRect;
        [MonoModIgnore] protected Rectangle? BButtonRect;
        [MonoModIgnore] protected Rectangle? XButtonRect;
        
        [MonoModIgnore] protected SoundEffect sScreenWiden;
        [MonoModIgnore] protected SoundEffect sScreenNarrowen;
        [MonoModIgnore] protected SoundEffect sSliderValueDecrease;
        [MonoModIgnore] protected SoundEffect sSliderValueIncrease;
        [MonoModIgnore] protected SoundEffect sStartGame;
        [MonoModIgnore] protected SoundEffect sAppear;
        [MonoModIgnore] protected SoundEffect sDisappear;
        [MonoModIgnore] protected SoundEffect sAdvanceLevel;
        [MonoModIgnore] protected SoundEffect sCancel;
        [MonoModIgnore] protected SoundEffect sConfirm;
        [MonoModIgnore] protected SoundEffect sCursorUp;
        [MonoModIgnore] protected SoundEffect sCursorDown;
        [MonoModIgnore] protected SoundEffect sExitGame;
        [MonoModIgnore] protected SoundEffect sReturnLevel;
        
        [MonoModIgnore] public bool CursorSelectable;
        [MonoModIgnore] public bool CursorClicking;
        [MonoModIgnore] protected float SinceMouseMoved = 3;
        [MonoModIgnore] protected SelectorPhase selectorPhase;
        
        [MonoModIgnore] protected List<MenuLevel> MenuLevels;
        [MonoModIgnore] protected /*LeaderboardsMenuLevel*/ MenuLevel LeaderboardsMenu;
        [MonoModIgnore] protected /*ControlsMenuLevel*/ MenuLevel ControlsMenu;
        [MonoModIgnore] public /*CreditsMenuLevel*/ MenuLevel CreditsMenu;
        [MonoModIgnore] protected MenuItem StereoMenuItem;
        [MonoModIgnore] protected MenuItem SinglethreadedMenuItem;
        [MonoModIgnore] protected MenuItem PauseOnLostFocusMenuItem;
        [MonoModIgnore] protected MenuLevel VideoSettingsMenu;
        [MonoModIgnore] protected MenuLevel CurrentMenuLevel;
        [MonoModIgnore] protected MenuLevel MenuRoot;
        [MonoModIgnore] protected MenuLevel UnlockNeedsLIVEMenu;
        [MonoModIgnore] protected MenuLevel HelpOptionsMenu;
        [MonoModIgnore] protected MenuLevel StartNewGameMenu;
        [MonoModIgnore] protected MenuLevel ExitToArcadeMenu;
        [MonoModIgnore] protected MenuLevel GameSettingsMenu;
        [MonoModIgnore] protected MenuLevel AudioSettingsMenu;
        [MonoModIgnore] protected /*SaveManagementLevel*/ MenuLevel SaveManagementMenu;
        [MonoModIgnore] protected MenuLevel lastMenuLevel;
        [MonoModIgnore] public MenuLevel nextMenuLevel;
        
        //lazy fields
        private float sinceRestartNoteShown;
        private float sinceRecommendedShown;

		public IContentManagerProvider CMProvider { [MonoModIgnore] protected get; [MonoModIgnore] set; }
		public IFontManager Fonts { [MonoModIgnore] protected get; [MonoModIgnore] set; }
		public IGameStateManager GameState { [MonoModIgnore] protected get; [MonoModIgnore] set; }
		public IInputManager InputManager { [MonoModIgnore] protected get; [MonoModIgnore] set; }
		public IKeyboardStateManager KeyboardState { [MonoModIgnore] protected get; [MonoModIgnore] set; }
		public IMouseStateManager MouseState { [MonoModIgnore] protected get; [MonoModIgnore] set; }
		public ISoundManager SoundManager { [MonoModIgnore] protected get; [MonoModIgnore] set; }
		public ITargetRenderingManager TargetRenderer { [MonoModIgnore] protected get; [MonoModIgnore] set; }
        
        //Custom menus go in here
        protected MenuLevel FEZModMenu;

		protected MenuBase(Game game) : base(game) {
            //no-op
		}
        
        protected extern void orig_LoadContent();
		protected override void LoadContent() {
            orig_LoadContent();
			ContentManager cm = this.CMProvider.Get(CM.Menu);
		}
        
        public extern void orig_Initialize();
		public override void Initialize() {
			orig_Initialize();
            
            MenuItem item;
            //Custom menus go in here
            
            FEZModMenu = new MenuLevel() {
                Title = "FEZModMenu",
                AButtonString = "MenuApplyWithGlyph",
                BButtonString = "MenuSaveWithGlyph",
                IsDynamic = true,
                Oversized = true,
                Parent = HelpOptionsMenu,
                OnReset = delegate() {
                },
                OnPostDraw = delegate(SpriteBatch batch, SpriteFont font, GlyphTextRenderer tr, float alpha) {
                    float scale = Fonts.SmallFactor * batch.GraphicsDevice.GetViewScale();
                    float y = (float) batch.GraphicsDevice.Viewport.Height / 2f - (float) batch.GraphicsDevice.Viewport.Height / 3.825f;
                    if ((
                        FEZModMenu.SelectedIndex == 0 ||
                        FEZModMenu.SelectedIndex == 1
                    ) && selectorPhase == SelectorPhase.Select) {
                        sinceRestartNoteShown = Math.Min(sinceRestartNoteShown + 0.05f, 1f);
                        tr.DrawCenteredString(batch, Fonts.Small, StaticText.GetString("RequiresRestart"), new Color(1f, 1f, 1f, alpha * sinceRestartNoteShown), new Vector2(0f, y), scale);
                    } else {
                        sinceRestartNoteShown = Math.Max(sinceRestartNoteShown - 0.1f, 0f);
                    }
                }
            };
            
            item = FEZModMenu.AddItem<string>("StreamAssetsDisk", delegate() {
                    //onSelect
                }, false,
                () => (FezEngineMod.CacheDisabled) ? "CARTRIDGE" : "RAM",
                delegate(string lastValue, int change) {
                    FezEngineMod.CacheDisabled = !FezEngineMod.CacheDisabled;
                }
            );
            item.UpperCase = true;
            
            item = FEZModMenu.AddItem<string>("StreamMusicType", delegate() {
                    //onSelect
                }, false,
                delegate() {
                    switch (FezEngineMod.MusicCache) {
                        case MusicCacheMode.Default:
                            return "DEFAULT MEDIUM";
                        case MusicCacheMode.Disabled:
                            return "RAM";
                        case MusicCacheMode.Enabled:
                            return "CARTRIDGE";
                    }
                    return "UNKNOWN";
                },
                delegate(string lastValue, int change) {
                    int val = (int) FezEngineMod.MusicCache;
                    if (val < (int) MusicCacheMode.Default) {
                        val = (int) MusicCacheMode.Default;
                    }
                    if (val > (int) MusicCacheMode.Enabled) {
                        val = (int) MusicCacheMode.Enabled;
                    }
                    FezEngineMod.MusicCache = (MusicCacheMode) val;
                }
            );
            item.UpperCase = true;
            
            HelpOptionsMenu.AddItem("FEZModMenu", delegate() {
                ChangeMenuLevel(FEZModMenu, false);
            });
            MenuLevels.Add(FEZModMenu);
        
            foreach (MenuLevel current in MenuLevels) {
				if (current != MenuRoot && current.Parent == null) {
					current.Parent = MenuRoot;
				}
			}
		}
        
        public extern void orig_Update(GameTime gameTime);
		public override void Update(GameTime gameTime) {
            orig_Update(gameTime);
        }

        private extern void orig_Select(MenuLevel activeLevel);
		private void Select(MenuLevel activeLevel) {
			orig_Select(activeLevel);
		}
        
        [MonoModIgnore]	protected virtual extern bool AllowDismiss();
        [MonoModIgnore]	protected virtual extern bool AlwaysShowBackButton();
        [MonoModIgnore]	private extern void ApplyVideo();
        [MonoModIgnore]	public extern bool ChangeMenuLevel(MenuLevel next, bool silent = false);
        [MonoModIgnore]	protected extern virtual void ContinueGame();
        [MonoModIgnore]	private extern void DestroyMenu();
        [MonoModIgnore]	protected override extern void Dispose(bool disposing);
        [MonoModIgnore]	public override extern void Draw(GameTime gameTime);
        [MonoModIgnore]	private extern void DrawButtons();
        [MonoModIgnore]	private extern void DrawLevel(MenuLevel level, bool toTexture);
        [MonoModIgnore]	private extern void DynamicUpgrade();
        [MonoModIgnore]	protected virtual extern void PostInitialize();
        [MonoModIgnore]	private extern void RenderToTexture();
        [MonoModIgnore]	private extern void Rescale();
        [MonoModIgnore]	protected virtual extern void ResumeGame();
        [MonoModIgnore]	protected virtual extern void ReturnToArcade();
        [MonoModIgnore]	private extern void ReturnToAudioDefault();
        [MonoModIgnore]	private extern void ReturnToGameDefault();
        [MonoModIgnore]	private extern void ReturnToVideoDefault();
        [MonoModIgnore]	private extern void ShowAchievements();
        [MonoModIgnore]	protected virtual extern void StartNewGame();
        [MonoModIgnore]	protected virtual extern bool UpdateEarlyOut();
        [MonoModIgnore]	private extern void UpdateSelector(float elapsedSeconds);
        [MonoModIgnore]	private extern void UpOneLevel(MenuLevel activeLevel);
	}
    
    //internal in FEZ
    [MonoModIgnore]
    public enum SelectorPhase {
		Appear,
		Disappear,
		Shrink,
		Grow,
		Select,
		FadeIn
	}
}
