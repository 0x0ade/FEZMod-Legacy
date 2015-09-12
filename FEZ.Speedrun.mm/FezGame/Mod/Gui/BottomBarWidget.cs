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
    public class BottomBarWidget : GuiWidget {

        public ButtonWidget TimeWidget;
        public ButtonWidget ProgressWidget;

        public BottomBarWidget(Game game) 
            : base(game) {
            Widgets.Add(TimeWidget = new ButtonWidget(Game, "00:00:00.0000") {
                Position = new Vector2(0f, 0f),
                Size = new Vector2(256f, 24f),
                UpdateBounds = false
            });
            Widgets.Add(ProgressWidget = new ButtonWidget(Game) {
                Position = new Vector2(0f, 0f),
                Size = new Vector2(GraphicsDevice.Viewport.Width - 256f, 24f),
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
                Size.Y = 24;
            }

            ProgressWidget.Background = GuiHandler.DefaultForeground;

            TimeWidget.Label = SpeedrunInfo.FormatTime(FezSpeedrun.Clock.Time.ToString()) + " ";
            TimeWidget.Position.X = 0f;
            TimeWidget.Position.Y = 0f;

            base.Update(gameTime);

            ProgressWidget.Position.X = TimeWidget.Size.X;
            ProgressWidget.Position.Y = 0f;
            ProgressWidget.Size.X = (Size.X - TimeWidget.Size.X) * (((float) ((TASComponent) GuiHandler).RewindPosition) / ((float) ((TASComponent) GuiHandler).RewindData.Count));
            ProgressWidget.Size.Y = Size.Y;
        }

    }
}

