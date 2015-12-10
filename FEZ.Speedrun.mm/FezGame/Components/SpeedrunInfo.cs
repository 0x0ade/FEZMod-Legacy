using FezGame.Mod;
using System;
using System.Collections.Generic;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FezGame.Speedrun;
using FezGame.Speedrun.Clocks;

namespace FezGame.Components {
    public class SpeedrunInfo : DrawableGameComponent {

        [ServiceDependency]
        public IGameService GameService { get; set; }
        [ServiceDependency]
        public IGameStateManager GameState { get; set; }
        [ServiceDependency]
        public IFontManager FontManager { get; set; }
        [ServiceDependency]
        public IGameLevelManager LevelManager { get; set; }
        [ServiceDependency]
        public ILevelMaterializer LevelMaterializer { get; set; }
        [ServiceDependency]
        public IPlayerManager PlayerManager { get; set; }

        public static SpeedrunInfo Instance;

        public SpriteBatch SpriteBatch { get; set; }
        public GlyphTextRenderer GTR { get; set; }

        public SpriteFont FontSmall;
        public float FontSmallFactor;
        public SpriteFont FontBig;
        public float FontBigFactor;

        protected string PrevLevel;

        public SpeedrunInfo(Game game)
            : base(game) {
            UpdateOrder = 1000;
            DrawOrder = 3000;
            Instance = this;
        }

        public override void Initialize() {
            base.Initialize();

            FontSmall = FontManager.Small;
            FontSmallFactor = 1f;
            FontBig = FontManager.Big;
            FontBigFactor = 1.5f;

            FezSpeedrun.DefaultSplitCases.Add(delegate(ISpeedrunClock clock) {
                if (LevelManager == null || LevelManager.Name == null) {
                    return null;
                }
                if (LevelManager.Name.StartsWith("GOMEZ_HOUSE_END_") && (PlayerManager.Action == ActionType.EnterDoorSpin || PlayerManager.Action == ActionType.EnteringDoor)) {
                    clock.Running = false;
                    return "ZE_DOOR_AT_ZE_END";
                }
                if (LevelManager.Name != PrevLevel) {
                    PrevLevel = LevelManager.Name;
                    return LevelManager.Name;
                }
                return null;
            });
        }
        
        protected override void LoadContent() {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            GTR = new GlyphTextRenderer(Game);
        }

        public override void Update(GameTime gameTime) {
            if (FezSpeedrun.Clock == null || !FezSpeedrun.Clock.Running) {
                PrevLevel = null;
                return;
            }

            FezSpeedrun.Clock.Update();
        }

        public override void Draw(GameTime gameTime) {
            if (!FEZMod.Preloaded || FezSpeedrun.Clock == null || !FezSpeedrun.Clock.InGame) {
                return;
            }
            
            Viewport viewport = GraphicsDevice.Viewport;
            float viewScale = GraphicsDevice.GetViewScale();
            
            GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
            SpriteBatch.BeginPoint();
            
            float lineBigHeight = FontBig.MeasureString("Time: 01:23:45.6789").Y * viewScale * FontBigFactor;
            GTR.DrawShadowedText(SpriteBatch, FontBig, "Time: "+FormatTime(FezSpeedrun.Clock.Time.ToString()), new Vector2(0, 0), Color.White, viewScale * FontBigFactor);

            if (FezSpeedrun.Display == SpeedrunDisplayMode.CurrentPerRoom) {
                List<Split> levelTimes = FezSpeedrun.Clock.Splits;
            
                float lineHeight = FontSmall.MeasureString("Time: 01:23:45.6789").Y * viewScale * FontSmallFactor;
                const int maxSplits = 6;
                for (int i = 0; i < levelTimes.Count && i < maxSplits; i++) {
                    Split split = levelTimes[(levelTimes.Count - 1) - i];
                    string text = split.Text;
                    TimeSpan time = split.Time;
                    GTR.DrawShadowedText(SpriteBatch, FontSmall, text + ": " + FormatTime(time.ToString()), new Vector2(0, lineBigHeight + i * lineHeight), new Color(Color.White, 1f - ((float)i) / ((float)maxSplits + 1f)), viewScale * FontSmallFactor);
                }
            }

            SpriteBatch.End();
        }

        protected static readonly char[] FormatTime_zero = new char[] {'0'};
        public static string FormatTime(string time, bool cutoffDots = false) {
            if (cutoffDots) {
                int dotIndex = time.IndexOf('.');
                if (dotIndex < 0) {
                    return time;
                }
                return time.Substring(0, time.IndexOf('.'));
            }
            time = time.TrimEnd(FormatTime_zero);
            if (time.EndsWith(":")) {
                time = time + "00";
            }
            return time;
        }

    }
}

