using System;
using FezEngine;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FezGame.Components;
using FezGame.Editor;

namespace FezGame.Mod.Gui {
    public class ArtObjectButtonWidget : AssetButtonWidget {

        public ArtObject AO;
        public Texture2D Cubemap;

        public ArtObjectButtonWidget(Game game) 
            : this(game, null) {
        }

        public ArtObjectButtonWidget(Game game, ArtObject ao)
            : base(game, ao.Name) {
            AO = ao;
            Cubemap = ao.Cubemap.MaxAlpha();
            
            Size.X = 128f;
            Size.Y = 128f;
        }

        public override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

            if (AO == null || !InView || Cubemap == null) {
                return;
            }

            GuiHandler.SpriteBatch.Draw(Cubemap, new Rectangle(
                (int) (Position.X + Offset.X),
                (int) (Position.Y + Offset.Y),
                128, 128
                ), new Rectangle(
                0, 0,
                Cubemap.Height, Cubemap.Height
                ), Color.White);
        }

        public override void Click(GameTime gameTime, int mb) {
            if (AO == null) {
                return;
            }

            if (mb == 1) {
                ((ILevelEditor) GuiHandler).Placing = AO;
            } else if (mb == 3) {
                //TODO show exact placement dialog
            }
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            
            if (Cubemap != null) {
                Cubemap.Dispose();
                Cubemap = null;
                AO.Dispose();
                AO = null;
            }
        }

    }
}

