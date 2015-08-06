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

namespace FezGame.Mod.Gui {
    public class WindowHeaderWidget : GuiWidget {

        public string Label;
        public SpriteFont Font;

        public ButtonWidget CloseButtonWidget { get; protected set; }

        protected Vector2 ParentPreDrag;

        public WindowHeaderWidget(Game game) 
            : this(game, null) {
        }

        public WindowHeaderWidget(Game game, string label) 
            : base(game) {
            Label = label;
            Font = FontManager.Small;

            Widgets.Add(CloseButtonWidget = new ButtonWidget(game));
            CloseButtonWidget.Label = " X";
            CloseButtonWidget.Background = Color.Red;
            CloseButtonWidget.Action = delegate() {
                if (Parent == null) {
                    return;
                }
                if (Parent.Parent != null) {
                    Parent.Parent.Widgets.Remove(Parent);
                } else {
                    GuiHandler.Widgets.Remove(Parent);
                }
            };
        }

        public override void Update(GameTime gameTime) {
            if (UpdateBounds) {
                if (Parent != null) {
                    Size.X = Parent.Size.X;
                    if (Parent is ContainerWidget) {
                        Label = ((ContainerWidget)Parent).Label;
                    }
                } else if (Label != null) {
                    float viewScale = SettingsManager.GetViewScale(GraphicsDevice);
                    Size.X = Font.MeasureString(Label).X * viewScale;
                }

                Size.Y = 24f;

                Position.X = 0;
                Position.Y = -Size.Y;

                CloseButtonWidget.UpdateBounds = false;
                CloseButtonWidget.Size.X = Size.Y;
                CloseButtonWidget.Size.Y = Size.Y;
            }

            float offset = 0f;
            for (int i = 0; i < Widgets.Count; i++) {
                Widgets[i].Update(gameTime);

                offset += Widgets[i].Size.X;

                Widgets[i].Position.X = Size.X - offset;
                Widgets[i].Position.Y = 0;
            }
        }

        public override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

            if (!InView || Label == null) {
                return;
            }

            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);
            GuiHandler.GTR.DrawShadowedText(GuiHandler.SpriteBatch, Font, Label, Position + Offset, Foreground, viewScale);
        }

        public override void Dragging(GameTime gameTime, MouseButtonStates state) {
            if (Parent == null || state == MouseButtonStates.DragEnded) {
                return;
            }

            if (state == MouseButtonStates.DragStarted) {
                ParentPreDrag = Parent.Position;
            }

            Parent.Position.X = ParentPreDrag.X + MouseState.LeftButton.DragState.Movement.X;
            Parent.Position.Y = ParentPreDrag.Y + MouseState.LeftButton.DragState.Movement.Y;
        }

    }
}

