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
using FezGame.Components;

namespace FezGame.Editor.Widgets {
    public class ButtonWidget : EditorWidget {

        public String Label;
        public Action Action;

        public SpriteFont Font;

        public ButtonWidget(Game game) 
            : base(game) {
            Font = FontManager.Small;
        }

        public ButtonWidget(Game game, String label) 
            : base(game) {
            Label = label;
            Font = FontManager.Small;
        }

        public ButtonWidget(Game game, String label, Action action) 
            : base(game) {
            Label = label;
            Action = action;
            Font = FontManager.Small;
        }

        public override void Update(GameTime gameTime) {
            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);
            Size.X = Font.MeasureString(Label).X * viewScale;
            Size.Y = 24;
        }

        public override void Draw(GameTime gameTime) {
            DrawBackground(gameTime);

            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);
            LevelEditor.GTR.DrawShadowedText(LevelEditor.SpriteBatch, Font, Label, Position + Offset, Color.White, viewScale);
        }

    }
}

