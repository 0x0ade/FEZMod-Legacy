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
    public class TrilePickerWidget : EditorWidget {

        [ServiceDependency]
        public IGameLevelManager LevelManager { get; set; }

        protected TrileSet TrileSetOld;

        public float ScrollMomentum = 0f;
        public float ScrollOffset = 0f;
        public float Width = 0f;

        protected Rectangle scrollIndicatorBounds = new Rectangle();

        public Texture2D TrileAtlas { get; set; }

        public TrilePickerWidget(Game game) 
            : base(game) {
        }

        public override void Update(GameTime gameTime) {
            if (TrileSetOld != LevelManager.TrileSet) {
                UpdateWidgets();
                TrileSetOld = LevelManager.TrileSet;
            }

            ScrollOffset += ScrollMomentum;
            ScrollMomentum *= 0.5f;
            if (ScrollOffset < 0f) {
                ScrollOffset = 0f;
            }
            if (ScrollOffset > Width) {
                ScrollOffset = Width;
            }

            if (UpdateBounds) {
                Size.X = GraphicsDevice.Viewport.Width;
                Size.Y = 36f;
            }

            Width = 0f;
            for (int i = 0; i < Widgets.Count; i++) {
                Widgets[i].Parent = this;
                Widgets[i].LevelEditor = LevelEditor;
                Widgets[i].Update(gameTime);

                Widgets[i].Position.X = Width - ScrollOffset;
                Widgets[i].Position.Y = 0;

                Width += Widgets[i].Size.X + 4f;
            }
        }

        public override void DrawBackground(GameTime gameTime) {
            base.DrawBackground(gameTime);

            if (!InView) {
                return;
            }

            scrollIndicatorBounds.X = backgroundBounds.X + (int) (Size.X * ScrollOffset / Width);
            scrollIndicatorBounds.Y = backgroundBounds.Y + (int) (Size.Y) - 4;
            scrollIndicatorBounds.Width = 8;
            scrollIndicatorBounds.Height = 4;

            LevelEditor.SpriteBatch.Draw(pixelTexture, scrollIndicatorBounds, new Color(255, 255, 255, Background.A));
        }

        public void UpdateWidgets() {
            Widgets.Clear();
            ScrollOffset = 0f;

            //WARNING: It is not performant as it reads the orig atlas from the GPU / VRAM, modifies it on the CPU and then pushes it back to VRAM.
            //TODO: Learn how to use FBOs in MonoDevelop / XNA.
            TrileAtlas = new Texture2D(GraphicsDevice, LevelManager.TrileSet.TextureAtlas.Width, LevelManager.TrileSet.TextureAtlas.Height);
            Color[] trileAtlasData = new Color[TrileAtlas.Width * TrileAtlas.Height];
            LevelManager.TrileSet.TextureAtlas.GetData(trileAtlasData);
            for (int i = 0; i < trileAtlasData.Length; i++) {
                trileAtlasData[i].A = 255;
            }
            TrileAtlas.SetData(trileAtlasData);

            foreach (Trile trile in LevelManager.TrileSet.Triles.Values) {
                TrileButtonWidget button = new TrileButtonWidget(Game, trile);
                button.TrileAtlas = TrileAtlas;
                Widgets.Add(button);
            }
        }

        public override void Scroll(GameTime gameTime, int turn) {
            ScrollMomentum -= turn * 128f;
        }

    }
}

