using System;
using Microsoft.Xna.Framework;

namespace FezGame.Mod.Gui {
    public class ContainerWidget : GuiWidget {

        public Func<object> RefreshValue;

        public string Label;

        public float Hovered = 0f;

        public float ScrollMomentum = 0f;
        public float ScrollOffset = 0f;

        public float InnerHeight { get; protected set; }
        protected Rectangle scrollIndicatorBounds = new Rectangle();

        public bool DoNotClipChildren = false;
        protected bool clipChildrenSet = false;

        public ContainerWidget(Game game, GuiWidget[] widgets = null)
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

            if (!DoNotClipChildren) {
                ClipChildren |= !clipChildrenSet;
            }

            ClipChildren &= !DoNotClipChildren;

            float innerHeight = 0f;
            foreach (GuiWidget widget in Widgets) {
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

            foreach (GuiWidget widget in Widgets) {
                if (widget is WindowHeaderWidget) {
                    continue;
                }

                widget.Draw(gameTime);
            }

            if (clippingChildren) {
                StopClipping();
            }

            foreach (GuiWidget widget in Widgets) {
                if (!(widget is WindowHeaderWidget)) {
                    continue;
                }

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

            GuiHandler.SpriteBatch.Draw(pixelTexture, scrollIndicatorBounds, new Color(255, 255, 255, Background.A));
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

