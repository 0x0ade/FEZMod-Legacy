using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Mod.Gui {
    public class TopBarWidget : GuiWidget {

        public TopBarWidget(Game game) 
            : base(game) {
        }

        public override void Update(GameTime gameTime) {
            if (UpdateBounds) {
                if (Parent != null) {
                    Size.X = Parent.Size.X;
                } else {
                    Size.X = GraphicsDevice.Viewport.Width;
                }
                Size.Y = 24;
            }

            float offset = 0f;
            for (int i = 0; i < Widgets.Count; i++) {
                Widgets[i].Update(gameTime);

                Widgets[i].Position.X = offset;
                Widgets[i].Position.Y = 0;

                Widgets[i].Background.A = 0;

                offset += Widgets[i].Size.X + 12f;
            }
        }

    }
}

