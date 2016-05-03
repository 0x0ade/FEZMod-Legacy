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
    public class TrileAllButtonWidget : ButtonWidget {

        [ServiceDependency]
        public IGameLevelManager LevelManager { get; set; }
        [ServiceDependency]
        public IPlayerManager PlayerManager { get; set; }

        public static Texture2D TexAll;
        public static Texture2D TexHideAll;

        public ButtonWidget Tooltip;

        public TrileAllButtonWidget(Game game) 
            : base(game) {
            Widgets.Add(Tooltip = new ButtonWidget(game, "All"));
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            TrilePickerWidget trilePicker = Parent as TrilePickerWidget;
            if (trilePicker == null) {
                return;
            }

            if (UpdateBounds) {
                Size.X = 32f;
                Size.Y = 32f;

                Tooltip.UpdateBounds = true;

                Tooltip.Position.X = -Tooltip.Size.X / 2f + Size.X / 2f;
                Tooltip.Position.Y = -Tooltip.Size.Y;
            }
        }

        public override void Draw(GameTime gameTime) {
            base.Draw(gameTime);
            
            TrilePickerWidget trilePicker = Parent as TrilePickerWidget;
            if (trilePicker == null || TexHideAll == null || TexAll == null || !InView) {
                return;
            }
            
            Texture2D tex = trilePicker.Large ? TexHideAll : TexAll;

            GuiHandler.SpriteBatch.Draw(tex, new Rectangle(
                (int) (Position.X + Offset.X),
                (int) (Position.Y + Offset.Y),
                32, 32
            ), Color.White);
        }

        public override void Click(GameTime gameTime, int mb) {
            TrilePickerWidget trilePicker = Parent as TrilePickerWidget;
            if (trilePicker == null) {
                return;
            }

            if (mb == 1) {
                trilePicker.Large = !trilePicker.Large;
            }
        }

    }
}

