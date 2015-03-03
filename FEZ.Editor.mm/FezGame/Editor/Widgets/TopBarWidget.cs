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
            ButtonWidget button;

            Widgets.Add(button = new ButtonWidget(Game, "File"));
            button.Widgets.Add(new ButtonWidget(Game, "New"));
            button.Widgets.Add(new ButtonWidget(Game, "Open"));
            button.Widgets.Add(new ButtonWidget(Game, "Save"));
            button.Background.A = 0;

            Widgets.Add(button = new ButtonWidget(Game, "View"));
            button.Background.A = 0;

            Widgets.Add(button = new ButtonWidget(Game, "Level"));
            button.Background.A = 0;

            Widgets.Add(button = new ButtonWidget(Game, "Trile Set"));
            button.Background.A = 0;

            Widgets.Add(button = new ButtonWidget(Game, "Trile"));
            button.Background.A = 0;

            Widgets.Add(button = new ButtonWidget(Game, "Strings"));
            button.Background.A = 0;

            Widgets.Add(button = new ButtonWidget(Game, "Scripting"));
            button.Background.A = 0;
        }

        public override void Update(GameTime gameTime) {
            Size.X = GraphicsDevice.Viewport.Width;
            Size.Y = 24;

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

