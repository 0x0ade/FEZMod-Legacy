using FezGame.Mod;
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
using FezGame.Speedrun;

namespace FezGame.Mod.Gui {
    public class QuickSaveWidget : GuiWidget {

        public static int DefaultHeight = 96;

        public QuickSave QuickSave;
        private float thumbnailShrinkedWidth = 0f;

        public QuickSaveWidget(Game game, QuickSave qs, float width = 256f) 
            : base(game) {
            Size = new Vector2(width, DefaultHeight);

            if (qs.Thumbnail != null) {
                thumbnailShrinkedWidth = (Size.Y / qs.Thumbnail.Height) * qs.Thumbnail.Width;
            }

            Widgets.Add(new ButtonWidget(Game, "Time: " + SpeedrunInfo.FormatTime(qs.Time.ToString())) {
                Position = new Vector2(0, 0f),
                UpdateBounds = true
            });
            Widgets.Add(new ButtonWidget(Game, qs.SaveData.Level) {
                Position = new Vector2(0, 24f),
                UpdateBounds = true
            });
            Widgets.Add(new ButtonWidget(Game, qs.SaveData.CubeShards + "/" + qs.SaveData.SecretCubes + "/" + qs.SaveData.CollectedParts + "/" + qs.SaveData.PiecesOfHeart) {
                Position = new Vector2(0, 48f),
                UpdateBounds = true
            });

            QuickSave = qs;
        }

        public override void Update(GameTime gameTime) {
            if (UpdateBounds && Parent != null) {
                Size.X = Parent.Size.X;
            }

            base.Update(gameTime);
        }

        public override void DrawBackground(GameTime gameTime) {
            base.DrawBackground(gameTime);

            if (QuickSave == null || !InView || QuickSave.Thumbnail == null) {
                return;
            }

            GuiHandler.SpriteBatch.Draw(QuickSave.Thumbnail, new Rectangle(
                (int) (Position.X + Offset.X),
                (int) (Position.Y + Offset.Y),
                (int) thumbnailShrinkedWidth, DefaultHeight
            ), null, Color.White);
        }

    }
}

