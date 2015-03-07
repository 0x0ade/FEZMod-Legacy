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
    public class TopBarWidget : EditorWidget {

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
                Widgets[i].Parent = this;
                Widgets[i].LevelEditor = LevelEditor;
                Widgets[i].Update(gameTime);

                Widgets[i].Position.X = offset;
                Widgets[i].Position.Y = 0;

                offset += Widgets[i].Size.X + 12f;
            }
        }

    }
}

