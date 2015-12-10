using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FezGame.Components;
using FezGame.Speedrun;

namespace FezGame.Mod.Gui {
    public class BottomBarWidget : GuiWidget {

        public ButtonWidget TimeWidget;
        public ButtonWidget FramesWidget;
        public ButtonWidget ProgressWidget;

        public BottomBarWidget(Game game) 
            : base(game) {
            Widgets.Add(TimeWidget = new ButtonWidget(Game, "T: 00:00:00.0000") {
                Position = new Vector2(0f, 0f),
                Size = new Vector2(192f, 24f),
                UpdateBounds = false
            });
            Widgets.Add(FramesWidget = new ButtonWidget(Game, "F: 0") {
                Position = new Vector2(GraphicsDevice.Viewport.Width - 192f, 0f),
                Size = new Vector2(192f, 24f),
                UpdateBounds = false
            });
            Widgets.Add(ProgressWidget = new ButtonWidget(Game) {
                Position = new Vector2(256f, 0f),
                Size = new Vector2(GraphicsDevice.Viewport.Width - 384f, 24f),
                Background = Color.White,
                UpdateBounds = false
            });
        }

        public override void Update(GameTime gameTime) {
            if (UpdateBounds) {
                if (Parent != null) {
                    Size.X = Parent.Size.X;
                } else {
                    Size.X = GraphicsDevice.Viewport.Width;
                }
                Size.Y = 24f * GraphicsDevice.GetViewScale();
            }

            ProgressWidget.Background = GuiHandler.DefaultForeground;
            
            string time = FezSpeedrun.Clock != null ? FezSpeedrun.Clock.Time.ToString() : "----";

            TimeWidget.Label = "T: " + SpeedrunInfo.FormatTime(time);
            TimeWidget.Size.X = 192f * GraphicsDevice.GetViewScale();
            TimeWidget.Size.Y = 24f * GraphicsDevice.GetViewScale();
            TimeWidget.Position.X = 0f;
            TimeWidget.Position.Y = 0f;

            FramesWidget.Label = " F: " + ((TASComponent) GuiHandler).RewindPosition;
            FramesWidget.Size.X = 192f * GraphicsDevice.GetViewScale();
            FramesWidget.Size.Y = 24f * GraphicsDevice.GetViewScale();
            FramesWidget.Position.X = Size.X - FramesWidget.Size.X;
            FramesWidget.Position.Y = 0f;

            base.Update(gameTime);

            ProgressWidget.Position.X = TimeWidget.Size.X;
            ProgressWidget.Position.Y = 0f;
            ProgressWidget.Size.X = (Size.X - TimeWidget.Size.X - FramesWidget.Size.X) * (((float) ((TASComponent) GuiHandler).RewindPosition) / ((float) ((TASComponent) GuiHandler).RewindData.Count));
            ProgressWidget.Size.Y = Size.Y;
        }

    }
}

