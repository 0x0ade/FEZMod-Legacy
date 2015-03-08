using Common;
using System;
using System.Collections.Generic;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using FezGame.Mod;

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

        public static SpeedrunInfo Instance;

        protected KeyValuePair<string, TimeSpan>[] tmpLevelTimes = new KeyValuePair<string, TimeSpan>[512];

        public SpriteBatch SpriteBatch { get; set; }
        public GlyphTextRenderer GTR { get; set; }

        public SpriteFont FontSmall;
        public float FontSmallFactor;
        public SpriteFont FontBig;
        public float FontBigFactor;

        public SpeedrunInfo(Game game)
            : base(game) {
            UpdateOrder = 1000;
            DrawOrder = 2000;
            Instance = this;
        }

        public override void Initialize() {
            base.Initialize();

            SpriteBatch = new SpriteBatch(GraphicsDevice);
            GTR = new GlyphTextRenderer(Game);

            FontSmall = FontManager.Small;
            FontSmallFactor = 1f;
            FontBig = FontManager.Big;
            FontBigFactor = 1.5f;
        }

        public override void Update(GameTime gameTime) {
            if (GameState.Loading || GameState.DotLoading || GameState.TimePaused || GameState.SaveData == null) {
                return;
            }

            SaveData saveData = GameState.SaveData;

            saveData.Set("Time", saveData.Get<TimeSpan>("Time") + gameTime.ElapsedGameTime);

            Dictionary<string, TimeSpan> levelTimes = saveData.Get<Dictionary<string, TimeSpan>>("LevelTimes");

            if (!levelTimes.ContainsKey(LevelManager.Name)) {
                levelTimes[LevelManager.Name] = new TimeSpan();
            }

            levelTimes[LevelManager.Name] += gameTime.ElapsedGameTime;

            saveData.Set("LevelTimes", levelTimes);
            
            GameState.SaveData = saveData;
        }

        public override void Draw(GameTime gameTime) {
            if (GameState.SaveData == null || !FEZMod.Preloaded) {
                return;
            }

            SaveData saveData = GameState.SaveData;

            Viewport viewport = GraphicsDevice.Viewport;
            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);

            GraphicsDeviceExtensions.SetBlendingMode(GraphicsDevice, BlendingMode.Alphablending);
            GraphicsDeviceExtensions.BeginPoint(SpriteBatch);

            float lineBigHeight = FontBig.MeasureString("Time: 01:23:45.6789").Y * viewScale * FontBigFactor;
            GTR.DrawShadowedText(SpriteBatch, FontBig, "Time: "+saveData.Get<TimeSpan>("Time"), new Vector2(0, 0), Color.White, viewScale * FontBigFactor);

            IDictionary<string, TimeSpan> levelTimes = saveData.Get<Dictionary<string, TimeSpan>>("LevelTimes");

            int levelTimesCount = levelTimes.Count;
            levelTimes.CopyTo(tmpLevelTimes.Length < levelTimesCount ? (tmpLevelTimes = new KeyValuePair<string, TimeSpan>[levelTimesCount]) : tmpLevelTimes, 0);

            float lineHeight = 24f;//FontSmall.MeasureString("Time: 01:23:45.6789").Y * viewScale * FontSmallFactor;
            for (int i = 0; i < levelTimesCount; i++) {
                string level = tmpLevelTimes[i].Key;
                TimeSpan time = tmpLevelTimes[i].Value;
                GTR.DrawShadowedText(SpriteBatch, FontSmall, level+": "+time, new Vector2(0, lineBigHeight + i * lineHeight), Color.White, viewScale * FontSmallFactor);
            }

            SpriteBatch.End();
        }

    }
}

