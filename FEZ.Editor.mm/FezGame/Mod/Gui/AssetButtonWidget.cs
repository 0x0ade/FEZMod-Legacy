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
    public abstract class AssetButtonWidget : ButtonWidget {

        public ButtonWidget Tooltip;

        public AssetButtonWidget(Game game, string tooltip) 
            : base(game) {
            Widgets.Add(Tooltip = new ButtonWidget(game, tooltip));
            Background.A = 0;
        }
        
        public override void Update(GameTime gameTime) {
            for (int i = 0; i < Widgets.Count; i++) {
                Widgets[i].Update(gameTime);
            }

            Hovered -= (float) gameTime.ElapsedGameTime.TotalSeconds;
            bool showedChildren = ShowChildren;
            ShowChildren = Hovered > 0f;
            if (!showedChildren && ShowChildren) {
                for (int i = 0; i < Widgets.Count; i++) {
                    Widgets[i].Refresh();
                }
            }
            
            if (UpdateBounds) {
                Tooltip.UpdateBounds = true;

                Tooltip.Position.X = -Tooltip.Size.X / 2f + Size.X / 2f;
                Tooltip.Position.Y = -Tooltip.Size.Y;
            }
        }

        public override void Dragging(GameTime gameTime, MouseButtonStates state) {
            if (Parent != null) {
                Parent.Dragging(gameTime, state);
            } else {
                base.Dragging(gameTime, state);
            }
        }
        
    }
}

