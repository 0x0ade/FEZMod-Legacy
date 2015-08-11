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

namespace FezGame.Mod.Gui {
    public class TextFieldWidget : ButtonWidget {

        [ServiceDependency]
        public IKeyboardStateManager KeyboardState { get; set; }

        public string Text = "";

        protected bool Focused = false;
        protected float BlinkTime = 0f;
        protected bool BlinkStatus = false;
        protected int CursorPosition = 0;
        protected float CursorScroll = 0f;

        protected bool PressedDEL = false;

        public TextFieldWidget(Game game) 
            : this(game, "") {
        }

        public TextFieldWidget(Game game, string text, string root) 
            : this(game, text) {
            Fill(root);
        }

        public TextFieldWidget(Game game, string text, IEnumerable<string> list) 
            : this(game, text) {
            Fill(list);
        }

        public TextFieldWidget(Game game, string text) 
            : base(game) {
            Text = text;
            Font = FontManager.Small;
            ShowChildren = false;
            Foreground = Color.Black;
            Background = Color.White;

            KeyboardState.RegisterKey(Keys.Delete);
        }

        public override void Update(GameTime gameTime) {
            if (UpdateBounds) {
                Size.Y = 24f;
            }

            if (Focused) {
                BlinkTime += (float) gameTime.ElapsedGameTime.TotalSeconds;

                if (KeyboardState.GetKeyState(Keys.Delete) == FezButtonState.Pressed && CursorPosition < Text.Length) {
                    Text = Text.Substring(0, CursorPosition) + Text.Substring(Math.Max(CursorPosition + 1, CursorPosition));
                }

                if (ParentAs<ContainerWidget>() != null) {
                    ParentAs<ContainerWidget>().Hover(gameTime);
                }
            }
            if (BlinkTime >= 0.5f) {
                BlinkTime -= 0.5f;
                //BlinkStatus = !BlinkStatus;
            }
            BlinkStatus = /*BlinkStatus && */Focused;

            if (InputManager.Left == FezButtonState.Pressed) {
                CursorPosition--;
            }
            if (InputManager.Right == FezButtonState.Pressed) {
                CursorPosition++;
            }

            CursorPosition = Math.Max(0, Math.Min(Text.Length, CursorPosition));

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
                Widgets[i].Update(gameTime);

                Widgets[i].Position.X = Size.X;
                Widgets[i].Position.Y = offset - ScrollOffset;

                Widgets[i].Background = Background;

                offset += Widgets[i].Size.Y;

                if (widthMax < Widgets[i].Size.X) {
                    widthMax = Widgets[i].Size.X;
                }
            }
            for (int i = 0; i < Widgets.Count; i++) {
                Widgets[i].Size.X = widthMax;
                Widgets[i].UpdateBounds = false;
            }

            Hovered -= (float) gameTime.ElapsedGameTime.TotalSeconds;
            bool showedChildren = ShowChildren;
            ShowChildren = Hovered > 0f;
            if (!showedChildren && ShowChildren) {
                for (int i = 0; i < Widgets.Count; i++) {
                    Widgets[i].Refresh();
                }
            }
        }

        public override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

            if (!InView || Text == null) {
                return;
            }

            StartClipping();

            CursorPosition = Math.Max(0, Math.Min(Text.Length, CursorPosition));

            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);

            float cursorOffset = Font.MeasureString((Text + "|").Substring(0, CursorPosition)).X;
            if (cursorOffset - CursorScroll >= Size.X * 0.75f) {
                CursorScroll = cursorOffset - Size.X * 0.75f;
            } else if (cursorOffset - CursorScroll <= Size.X * 0.25f) {
                CursorScroll = cursorOffset - Size.X * 0.25f;
            }
            CursorScroll = Math.Min(Font.MeasureString(Text + "|").X, CursorScroll);
            CursorScroll = Math.Max(0f, CursorScroll);

            GuiHandler.GTR.DrawShadowedText(GuiHandler.SpriteBatch, Font, Text.Substring(0, CursorPosition) + (BlinkStatus ? "|" : "") + Text.Substring(CursorPosition), Position + Offset - new Vector2(CursorScroll, 0f), Color.Black, viewScale);

            StopClipping();
        }

        public override void Click(GameTime gameTime, int mb) {
            if (mb != 1) {
                return;
            }
            Focused = true;
            BlinkTime = 0f;
            BlinkStatus = true;
            CursorPosition = Text.Length;
        }

        public override void Unfocus(GameTime gameTime) {
            Focused = false;
            BlinkStatus = false;
        }

        public override void TextInput(char c) {
            if (c == '\b' && CursorPosition != 0) {
                Text = Text.Substring(0, Math.Max(0, CursorPosition - 1)) + Text.Substring(CursorPosition);
                CursorPosition--;
            }
            if ((c == '\n' || c == '\r') && Action != null) {
                GuiHandler.Scheduled.Add(Action);
            }
            if (char.IsControl(c)) {
                return;
            }
            Text = Text.Substring(0, CursorPosition) + c + Text.Substring(CursorPosition);
            CursorPosition++;
        }

        public void Fill(string root) {
            Widgets.Clear();
            IEnumerable<string> list = CMProvider.GetAllIn(root);
            List<string> added = new List<string>();
            foreach (string item_ in list) {
                string item = item_.Substring(root.Length + 1).ToUpper();
                if (item.Contains("\\")) {
                    item = item.Substring(0, item.IndexOf('\\'));
                }
                if (item.Contains("/")) {
                    item = item.Substring(0, item.IndexOf('/'));
                }
                if (added.Contains(item)) {
                    continue;
                }
                added.Add(item);
                Widgets.Add(new ButtonWidget(Game, item, delegate() {
                    Text = item;
                }) {
                    Background = Background
                });
            }
        }

        public void Fill(IEnumerable<string> list) {
            Widgets.Clear();
            foreach (string item in list) {
                Widgets.Add(new ButtonWidget(Game, item, delegate() {
                    Text = item;
                }) {
                    Background = Background
                });
            }
        }

        public override void Refresh() {
            if (RefreshValue == null) {
                base.Refresh();
                return;
            }
            Text = (string) RefreshValue();
            Func<object> refreshValueOrig = RefreshValue;
            RefreshValue = null;
            base.Refresh();
            RefreshValue = refreshValueOrig;
        }

    }
}

