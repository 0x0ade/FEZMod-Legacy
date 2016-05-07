using System;
using FezEngine;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FezGame.Components;

namespace FezGame.Mod.Gui {
    public class TrileButtonWidget : AssetButtonWidget {

        [ServiceDependency]
        public IGameLevelManager LevelManager { get; set; }
        [ServiceDependency]
        public IPlayerManager PlayerManager { get; set; }

        public Trile Trile;
        public Texture2D TrileAtlas;

        public TrileButtonWidget(Game game) 
            : this(game, null) {
        }

        public TrileButtonWidget(Game game, Trile trile) 
            : base(game, trile.Name) {
            Trile = trile;
            
            Size.X = 32f;
            Size.Y = 32f;
        }

        public override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

            if (Trile == null || !InView || TrileAtlas == null) {
                return;
            }

            GuiHandler.SpriteBatch.Draw(TrileAtlas, new Rectangle(
                (int) (Position.X + Offset.X),
                (int) (Position.Y + Offset.Y),
                32, 32
                ), new Rectangle(
                (int) Math.Ceiling(((double)Trile.AtlasOffset.X) * ((double)TrileAtlas.Width)) + 1,
                (int) Math.Ceiling(((double)Trile.AtlasOffset.Y) * ((double)TrileAtlas.Height)) + 1,
                16, 16
                ), Color.White);
        }

        public override void Click(GameTime gameTime, int mb) {
            if (Trile == null) {
                return;
            }

            if (mb == 1) {
                ((ILevelEditor) GuiHandler).Placing = Trile;
            } else if (mb == 3) {
                GuiHandler.Scheduled.Add(() => ((ILevelEditor) GuiHandler).ShowTrilePlacementWindow(Trile.Id));
            }
        }
        
        //Disposing handled by level change

    }
}

