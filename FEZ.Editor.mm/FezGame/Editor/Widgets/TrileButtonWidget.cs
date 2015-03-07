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
    public class TrileButtonWidget : ButtonWidget {

        public Trile Trile;
        public Texture2D TrileAtlas { get; set; }

        public ButtonWidget Tooltip;

        public TrileButtonWidget(Game game) 
            : this(game, null) {
        }

        public TrileButtonWidget(Game game, Trile trile) 
            : base(game) {
            Trile = trile;
            Widgets.Add(Tooltip = new ButtonWidget(game));
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            if (Trile == null) {
                return;
            }

            if (UpdateBounds) {
                Size.X = 32f;
                Size.Y = 32f;
            }

            Tooltip.Label = Trile.Name;

            Tooltip.Position.X = -Tooltip.Size.X / 2f + Size.X / 2f;
            Tooltip.Position.Y = -Tooltip.Size.Y;
        }

        public override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

            if (Trile == null || !InView || TrileAtlas == null) {
                return;
            }

            LevelEditor.SpriteBatch.Draw(TrileAtlas, new Rectangle(
                (int) (Position.X + Offset.X),
                (int) (Position.Y + Offset.Y),
                32, 32
                ), new Rectangle(
                (int) Math.Ceiling(((double)Trile.AtlasOffset.X) * ((double)TrileAtlas.Width)) + 1,
                (int) Math.Ceiling(((double)Trile.AtlasOffset.Y) * ((double)TrileAtlas.Height)) + 1,
                16, 16
                ), Color.White);
        }

        public override void Clicked(GameTime gameTime) {
            if (Trile == null) {
                return;
            }

            LevelEditor.TrileId = Trile.Id;
        }

    }
}

