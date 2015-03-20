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
    public class TextFieldWidget : ButtonWidget {

        [ServiceDependency]
        public IKeyboardStateManager KeyboardState { get; set; }

        public String Text = "";

        protected bool Focused = false;
        protected float BlinkTime = 0f;
        protected bool BlinkStatus = false;

        public float ScrollMomentum = 0f;
        public float ScrollOffset = 0f;

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

            if (ShowChildren && Widgets.Count > 0) {
                ScrollOffset += ScrollMomentum;
                ScrollMomentum *= 0.5f;
                if (ScrollOffset < 0f) {
                    ScrollOffset = 0f;
                }
                if (ScrollOffset > Widgets[0].Size.Y * (Widgets.Count - 1)) {
                    ScrollOffset = Widgets[0].Size.Y * (Widgets.Count - 1);
                }
            } else {
                ScrollOffset = 0f;
                ScrollMomentum = 0f;
            }

            float offset = 0f;
            float widthMax = 0f;
            for (int i = 0; i < Widgets.Count; i++) {
                Widgets[i].Parent = this;
                Widgets[i].LevelEditor = LevelEditor;
                Widgets[i].Update(gameTime);

                Widgets[i].Position.X = Size.X;
                Widgets[i].Position.Y = offset - ScrollOffset;

                offset += Widgets[i].Size.Y;

                if (widthMax < Widgets[i].Size.X) {
                    widthMax = Widgets[i].Size.X;
                }
            }
            for (int i = 0; i < Widgets.Count; i++) {
                Widgets[i].Size.X = widthMax;
            }

            Hovered -= (float) gameTime.ElapsedGameTime.TotalSeconds;
            ShowChildren = Hovered > 0f;
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
            if (mb == 3) {
                Hovered = 0.1f;
            }
            if (mb != 1) {
                return;
            }
            Focused = true;
            BlinkTime = 0f;
            BlinkStatus = true;
        }

        public override void Hover(GameTime gameTime) {
            if (Hovered >= 0f) {
                Hovered = 0.1f;
            }
        }

        public override void Scroll(GameTime gameTime, int turn) {
            ScrollMomentum -= turn * 128f;
        }

        public override void Unfocus(GameTime gameTime) {
            Focused = false;
            BlinkStatus = false;
        }

        public override void TextInput(char c) {
            if (c == '\b') {
                Text = Text.Substring(0, Math.Max(Text.Length - 1, 0));
            }
            if (c == '\n' && Action != null) {
                LevelEditor.Scheduled.Add(Action);
            }
            if (char.IsControl(c)) {
                return;
            }
            Text += c;
        }

        public void Fill(String root) {
            Widgets.Clear();
            IEnumerable<string> list = CMProvider.GetAllIn(root);
            foreach (string item_ in list) {
                string item = item_.Substring(root.Length + 1).ToUpper();
                if (item.Contains("\\") || item.Contains("/")) {
                    continue;
                }
                ButtonWidget button;
                Widgets.Add(button = new ButtonWidget(Game, item, delegate() {
                    Text = item;
                }));
                button.Background = Background;
            }
        }

        public void Fill(IEnumerable<string> list) {
            Widgets.Clear();
            foreach (string item in list) {
                ButtonWidget button;
                Widgets.Add(button = new ButtonWidget(Game, item, delegate() {
                    Text = item;
                }));
                button.Background = Background;
            }
        }

    }
}

