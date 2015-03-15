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
        public SpriteFont Font;
        public bool LabelCentered = false;

        public Action Action;

        public float Hovered = 0f;

        public ButtonWidget(Game game) 
            : this(game, null) {
        }

        public ButtonWidget(Game game, String label) 
            : this(game, label, null) {
        }

        public ButtonWidget(Game game, String label, Action action) 
            : base(game) {
            Label = label;
            Action = action;
            Font = FontManager.Small;
            ShowChildren = false;
        }

        public override void Update(GameTime gameTime) {
            if (UpdateBounds && Label != null) {
                float viewScale = SettingsManager.GetViewScale(GraphicsDevice);
                Size.X = Font.MeasureString(Label).X * viewScale;
                Size.Y = 24f;
            }

            float offset = Size.Y;
            float widthMax = 0f;
            for (int i = 0; i < Widgets.Count; i++) {
                Widgets[i].Parent = this;
                Widgets[i].LevelEditor = LevelEditor;
                Widgets[i].Update(gameTime);

                Widgets[i].Position.X = 0;
                Widgets[i].Position.Y = offset;

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

            if (!InView || Label == null) {
                return;
            }

            Vector2 offset = new Vector2(0f, 0f);
            if (LabelCentered) {
                offset.X = Size.X / 2f - Font.MeasureString(Label).X / 2f;
            }

            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);
            LevelEditor.GTR.DrawShadowedText(LevelEditor.SpriteBatch, Font, Label, Position + Offset + offset, Color.White, viewScale);
        }

        public override void Click(GameTime gameTime, int mb) {
            if (mb == 1 && Action != null) {
                LevelEditor.Scheduled.Add(Action);
            }
        }

        public override void Hover(GameTime gameTime) {
            Hovered = 0.1f;
            if (Parent is ButtonWidget && ((ButtonWidget)Parent).Hovered > 0f) {
                ((ButtonWidget)Parent).Hover(gameTime);
            }
        }

    }
}

