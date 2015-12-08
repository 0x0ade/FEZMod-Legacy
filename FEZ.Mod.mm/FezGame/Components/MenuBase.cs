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
        
        //Note: Everything marked with /*p1*/ was protected, with /*p2*/ was private.

        [MonoModIgnore] /*p1*/ public SpriteBatch SpriteBatch;
        [MonoModIgnore] /*p1*/ public GlyphTextRenderer tr;
        
        [MonoModIgnore] /*p1*/ public RenderTarget2D CurrentMenuLevelTexture;
        [MonoModIgnore] /*p1*/ public RenderTarget2D NextMenuLevelTexture;
        
        [MonoModIgnore] /*p2*/ public Texture2D CanClickCursor;
        [MonoModIgnore] /*p2*/ public Texture2D PointerCursor;
        [MonoModIgnore] /*p2*/ public Texture2D ClickedCursor;
        
        [MonoModIgnore] /*p1*/ public Mesh MenuLevelOverlay;
        [MonoModIgnore] /*p1*/ public Mesh Mask;
        [MonoModIgnore] /*p1*/ public Mesh Frame;
        [MonoModIgnore] /*p1*/ public Mesh Selector;
        
        [MonoModIgnore] /*p1*/ public Rectangle? AButtonRect;
        [MonoModIgnore] /*p1*/ public Rectangle? BButtonRect;
        [MonoModIgnore] /*p1*/ public Rectangle? XButtonRect;
        
        [MonoModIgnore] /*p1*/ public SoundEffect sScreenWiden;
        [MonoModIgnore] /*p1*/ public SoundEffect sScreenNarrowen;
        [MonoModIgnore] /*p1*/ public SoundEffect sSliderValueDecrease;
        [MonoModIgnore] /*p1*/ public SoundEffect sSliderValueIncrease;
        [MonoModIgnore] /*p1*/ public SoundEffect sStartGame;
        [MonoModIgnore] /*p1*/ public SoundEffect sAppear;
        [MonoModIgnore] /*p1*/ public SoundEffect sDisappear;
        [MonoModIgnore] /*p1*/ public SoundEffect sAdvanceLevel;
        [MonoModIgnore] /*p1*/ public SoundEffect sCancel;
        [MonoModIgnore] /*p1*/ public SoundEffect sConfirm;
        [MonoModIgnore] /*p1*/ public SoundEffect sCursorUp;
        [MonoModIgnore] /*p1*/ public SoundEffect sCursorDown;
        [MonoModIgnore] /*p1*/ public SoundEffect sExitGame;
        [MonoModIgnore] /*p1*/ public SoundEffect sReturnLevel;
        
        [MonoModIgnore] public bool CursorSelectable;
        [MonoModIgnore] public bool CursorClicking;
        [MonoModIgnore] /*p1*/ public float SinceMouseMoved = 3;
        [MonoModIgnore] /*p1*/ public SelectorPhase selectorPhase;
        
        [MonoModIgnore] /*p1*/ public List<MenuLevel> MenuLevels;
        [MonoModIgnore] /*p1*/ public /*LeaderboardsMenuLevel*/ MenuLevel LeaderboardsMenu;
        [MonoModIgnore] /*p1*/ public /*ControlsMenuLevel*/ MenuLevel ControlsMenu;
        [MonoModIgnore] public /*CreditsMenuLevel*/ MenuLevel CreditsMenu;
        [MonoModIgnore] /*p1*/ public MenuLevel VideoSettingsMenu;
        [MonoModIgnore] /*p1*/ public MenuLevel CurrentMenuLevel;
        [MonoModIgnore] /*p1*/ public MenuLevel MenuRoot;
        [MonoModIgnore] /*p1*/ public MenuLevel UnlockNeedsLIVEMenu;
        [MonoModIgnore] /*p1*/ public MenuLevel HelpOptionsMenu;
        [MonoModIgnore] /*p1*/ public MenuLevel StartNewGameMenu;
        [MonoModIgnore] /*p1*/ public MenuLevel ExitToArcadeMenu;
        [MonoModIgnore] /*p1*/ public MenuLevel GameSettingsMenu;
        [MonoModIgnore] /*p1*/ public MenuLevel AudioSettingsMenu;
        [MonoModIgnore] /*p1*/ public /*SaveManagementLevel*/ MenuLevel SaveManagementMenu;
        [MonoModIgnore] /*p1*/ public MenuLevel lastMenuLevel;
        [MonoModIgnore] public MenuLevel nextMenuLevel;
        
        //lazy fields
        /*p2*/ public float sinceRestartNoteShown;
        /*p2*/ public float sinceRecommendedShown;

		public IContentManagerProvider CMProvider { [MonoModIgnore] /*p1*/ get; [MonoModIgnore] set; }
		public IFontManager Fonts { [MonoModIgnore] /*p1*/ get; [MonoModIgnore] set; }
		public IGameStateManager GameState { [MonoModIgnore] /*p1*/ get; [MonoModIgnore] set; }
		public IInputManager InputManager { [MonoModIgnore] /*p1*/ get; [MonoModIgnore] set; }
		public IKeyboardStateManager KeyboardState { [MonoModIgnore] /*p1*/ get; [MonoModIgnore] set; }
		public IMouseStateManager MouseState { [MonoModIgnore] /*p1*/ get; [MonoModIgnore] set; }
		public ISoundManager SoundManager { [MonoModIgnore] /*p1*/ get; [MonoModIgnore] set; }
		public ITargetRenderingManager TargetRenderer { [MonoModIgnore] /*p1*/ get; [MonoModIgnore] set; }
        
		/*p1*/ public MenuBase(Game game) : base(game) {
            //no-op
		}
        
        public extern void orig_Initialize();
		public override void Initialize() {
			orig_Initialize();
            
            FEZMod.InitializeMenu(this);
        
            foreach (MenuLevel current in MenuLevels) {
				if (current != MenuRoot && current.Parent == null) {
					current.Parent = MenuRoot;
				}
			}
		}
        
		[MonoModIgnore] public override extern void Update(GameTime gameTime);
		[MonoModIgnore] /*p2*/ public extern void Select(MenuLevel activeLevel);
        [MonoModIgnore] protected override extern void LoadContent();
        [MonoModIgnore]	/*p1*/ public virtual extern bool AllowDismiss();
        [MonoModIgnore]	/*p1*/ public virtual extern bool AlwaysShowBackButton();
        [MonoModIgnore]	/*p2*/ public extern void ApplyVideo();
        [MonoModIgnore]	public extern bool ChangeMenuLevel(MenuLevel next, bool silent = false);
        [MonoModIgnore]	/*p1*/ public extern virtual void ContinueGame();
        [MonoModIgnore]	/*p2*/ public extern void DestroyMenu();
        [MonoModIgnore]	protected override extern void Dispose(bool disposing);
        [MonoModIgnore]	public override extern void Draw(GameTime gameTime);
        [MonoModIgnore]	/*p2*/ public extern void DrawButtons();
        [MonoModIgnore]	/*p2*/ public extern void DrawLevel(MenuLevel level, bool toTexture);
        [MonoModIgnore]	/*p2*/ public extern void DynamicUpgrade();
        [MonoModIgnore]	/*p1*/ public virtual extern void PostInitialize();
        [MonoModIgnore]	/*p2*/ public extern void RenderToTexture();
        [MonoModIgnore]	/*p2*/ public extern void Rescale();
        [MonoModIgnore]	/*p1*/ public virtual extern void ResumeGame();
        [MonoModIgnore]	/*p1*/ public virtual extern void ReturnToArcade();
        [MonoModIgnore]	/*p2*/ public extern void ReturnToAudioDefault();
        [MonoModIgnore]	/*p2*/ public extern void ReturnToGameDefault();
        [MonoModIgnore]	/*p2*/ public extern void ReturnToVideoDefault();
        [MonoModIgnore]	/*p2*/ public extern void ShowAchievements();
        [MonoModIgnore]	/*p1*/ public virtual extern void StartNewGame();
        [MonoModIgnore]	/*p1*/ public virtual extern bool UpdateEarlyOut();
        [MonoModIgnore]	/*p2*/ public extern void UpdateSelector(float elapsedSeconds);
        [MonoModIgnore]	/*p2*/ public extern void UpOneLevel(MenuLevel activeLevel);
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
