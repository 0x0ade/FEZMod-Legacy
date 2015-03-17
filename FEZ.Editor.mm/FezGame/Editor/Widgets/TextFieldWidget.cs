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
using Microsoft.Xna.Framework.Input;
using FezGame.Components;

namespace FezGame.Editor.Widgets {
    public class TextFieldWidget : EditorWidget {

        [ServiceDependency]
        public IKeyboardStateManager KeyboardState { get; set; }

        public String Text = "";
        public SpriteFont Font;

        protected bool Focused = false;
        protected float BlinkTime = 0f;
        protected bool BlinkStatus = false;

        public TextFieldWidget(Game game) 
            : this(game, "") {
        }

        public TextFieldWidget(Game game, String text) 
            : base(game) {
            Text = text;
            Font = FontManager.Small;
            ShowChildren = false;
            Foreground = Color.Black;
            Background = Color.White;
        }

        public override void Update(GameTime gameTime) {
            if (UpdateBounds) {
                Size.Y = 24f;
            }

            if (Focused) {
                BlinkTime += (float) gameTime.ElapsedGameTime.TotalSeconds;
            }
            if (BlinkTime >= 0.5f) {
                BlinkTime -= 0.5f;
                BlinkStatus = !BlinkStatus;
            }
            BlinkStatus = BlinkStatus && Focused;

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

            if (!InView || Text == null) {
                return;
            }

            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);
            LevelEditor.GTR.DrawShadowedText(LevelEditor.SpriteBatch, Font, Text + (BlinkStatus ? "|" : ""), Position + Offset, Color.Black, viewScale);
        }

        public override void Click(GameTime gameTime, int mb) {
            if (mb != 1) {
                return;
            }
            Focused = true;
            BlinkTime = 0f;
            BlinkStatus = true;
        }

        public override void Unfocus(GameTime gameTime) {
            Focused = false;
            BlinkStatus = false;
        }

        public override void TextInput(char c) {
            if (c == '\b') {
                Text = Text.Substring(0, Math.Max(Text.Length - 1, 0));
            }
            if (char.IsControl(c)) {
                return;
            }
            Text += c;
        }

    }
}

