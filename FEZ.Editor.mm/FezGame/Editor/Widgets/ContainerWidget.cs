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
    public class ContainerWidget : EditorWidget {

        public Func<object> RefreshValue;

        public string Label;

        public float Hovered = 0f;

        public float ScrollMomentum = 0f;
        public float ScrollOffset = 0f;

        public float InnerHeight { get; protected set; }
        protected Rectangle scrollIndicatorBounds = new Rectangle();

        public ContainerWidget(Game game, EditorWidget[] widgets = null)
            : base(game) {
            if (widgets != null) {
                Widgets.AddRange(widgets);
            }
            UpdateBounds = GetType() != typeof(ContainerWidget);
        }

        public override void Update(GameTime gameTime) {
            if (!UpdateBounds) {
                base.Update(gameTime);
                return;
            }

            ClipChildren = true;

            float innerHeight = 0f;
            foreach (EditorWidget widget in Widgets) {
                if (widget is WindowHeaderWidget) {
                    continue;
                }
                innerHeight += widget.Size.Y;
            }
            InnerHeight = innerHeight;

            ScrollOffset += ScrollMomentum;
            ScrollMomentum *= 0.5f;
            if (ScrollOffset < 0f) {
                ScrollOffset = 0f;
            }
            if (ScrollOffset > innerHeight - Size.Y) {
                ScrollOffset = innerHeight - Size.Y;
            }

            float offset = 0f;
            for (int i = 0; i < Widgets.Count; i++) {
                Widgets[i].Parent = this;
                Widgets[i].LevelEditor = LevelEditor;
                Widgets[i].Update(gameTime);

                if (Widgets[i].GetType() == typeof(ContainerWidget)) {
                    Widgets[i].ClipChildren = true;
                }

                if (Widgets[i] is WindowHeaderWidget) {
                    continue;
                }

                Widgets[i].Position.X = 0f;
                Widgets[i].Position.Y = offset - ScrollOffset;

                offset += Widgets[i].Size.Y;
            }
        }

        public override void Draw(GameTime gameTime) {
            if (!UpdateBounds) {
                base.Draw(gameTime);
                return;
            }

            DrawBackground(gameTime);

            if (!InView || !ShowChildren) {
                return;
            }

            bool clippingChildren = ClipChildren;

            if (clippingChildren) {
                StartClipping();
            }

            foreach (EditorWidget widget in Widgets) {
                if (widget is WindowHeaderWidget) {
                    continue;
                }

                widget.Parent = this;
                widget.LevelEditor = LevelEditor;
                widget.Draw(gameTime);
            }

            if (clippingChildren) {
                StopClipping();
            }

            foreach (EditorWidget widget in Widgets) {
                if (!(widget is WindowHeaderWidget)) {
                    continue;
                }

                widget.Parent = this;
                widget.LevelEditor = LevelEditor;
                widget.Draw(gameTime);
            }
        }

        public override void DrawBackground(GameTime gameTime) {
            base.DrawBackground(gameTime);

            if (!InView || !UpdateBounds) {
                return;
            }

            if (InnerHeight <= Size.Y) {
                return;
            }

            scrollIndicatorBounds.X = backgroundBounds.X + backgroundBounds.Width - 2;
            scrollIndicatorBounds.Y = backgroundBounds.Y + (int) (Size.Y * ScrollOffset / InnerHeight);
            scrollIndicatorBounds.Width = 4;
            scrollIndicatorBounds.Height = (int) (Size.Y * Size.Y / InnerHeight);

            LevelEditor.SpriteBatch.Draw(pixelTexture, scrollIndicatorBounds, new Color(255, 255, 255, Background.A));
        }

        public override void Hover(GameTime gameTime) {
            Hovered = 0.1f;
            base.Hover(gameTime);
        }

        public override void Unfocus(GameTime gameTime) {
            Hovered = 0f;
        }

        public override void Scroll(GameTime gameTime, int turn) {
            ScrollMomentum -= turn * 48f;
            base.Scroll(gameTime, turn);
        }

        public override void Refresh() {
            if (RefreshValue == null) {
                base.Refresh();
                return;
            }
            RefreshValue();
            base.Refresh();
        }

    }
}

