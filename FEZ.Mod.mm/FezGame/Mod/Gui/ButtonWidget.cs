using System;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Mod.Gui {
    public class ButtonWidget : ContainerWidget {

        public SpriteFont Font;
        public bool LabelCentered = false;
        public Vector2 LabelOffset = new Vector2(0f, 0f);

        public Action Action;

        public ButtonWidget(Game game) 
            : this(game, null, (Action) null) {
        }

        public ButtonWidget(Game game, string label, GuiWidget[] widgets, Action action = null) 
            : this(game, label, action) {
            Widgets.AddRange(widgets);
        }

        public ButtonWidget(Game game, string label, Action action = null) 
            : base(game) {
            Label = label;
            Action = action;
            Font = FontManager.Small;
            ShowChildren = false;
        }

        public override void Update(GameTime gameTime) {
            if (UpdateBounds) {
                if (Label != null) {
                    float viewScale = SettingsManager.GetViewScale(GraphicsDevice);
                    Size.X = Font.MeasureString(Label).X * viewScale + 4f;
                }
                Size.Y = 24f;
            }

            float offset = ParentAs<ContainerWidget>() != null ? 0f : Size.Y;
            float widthMax = 0f;
            for (int i = 0; i < Widgets.Count; i++) {
                Widgets[i].Update(gameTime);

                if (ParentAs<ContainerWidget>() != null) {
                    Widgets[i].Position.X = Size.X;
                } else {
                    Widgets[i].Position.X = 0f;
                }

                Widgets[i].Position.Y = offset;

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

            if (!InView || Label == null) {
                return;
            }

            Vector2 offset = LabelOffset;
            if (LabelCentered) {
                offset.X += Size.X / 2f - Font.MeasureString(Label).X / 2f;
            }

            StartClipping();

            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);
            GuiHandler.GTR.DrawShadowedText(GuiHandler.SpriteBatch, Font, Label, Position + Offset + offset, Foreground, viewScale);

            StopClipping();
        }

        public override void Click(GameTime gameTime, int mb) {
            if (mb == 1 && Action != null) {
                GuiHandler.Scheduled.Add(Action);
            }
        }

        public override void Refresh() {
            if (RefreshValue == null) {
                base.Refresh();
                return;
            }
            Label = (string) RefreshValue();
            base.Refresh();
        }

    }
}

