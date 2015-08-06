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
    public class CheckboxWidget : ButtonWidget {

        public bool Checked = false;

        public CheckboxWidget(Game game) 
            : this(game, "", false) {
        }

        public CheckboxWidget(Game game, string label, bool checked_ = false) 
            : base(game, label) {
            Checked = checked_;
            Font = FontManager.Small;
            ShowChildren = false;
        }

        public override void Update(GameTime gameTime) {
            if (UpdateBounds) {
                Size.Y = 24f;
            }
            LabelOffset.X = 28f;
        }

        public override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

            if (!InView) {
                return;
            }

            backgroundBounds.Width = backgroundBounds.Height = 24;
            GuiHandler.SpriteBatch.Draw(pixelTexture, backgroundBounds, Color.White);

            if (!Checked) {
                return;
            }

            StartClipping();

            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);

            GuiHandler.GTR.DrawShadowedText(GuiHandler.SpriteBatch, Font, " X", Position + Offset, Color.Black, viewScale);

            StopClipping();
        }

        public override void Click(GameTime gameTime, int mb) {
            if (mb != 1) {
                return;
            }
            Checked = !Checked;
        }

        public override void Refresh() {
            if (RefreshValue == null) {
                base.Refresh();
                return;
            }
            Checked = (bool) RefreshValue();
            Func<object> refreshValueOrig = RefreshValue;
            RefreshValue = null;
            base.Refresh();
            RefreshValue = refreshValueOrig;
        }

    }
}

