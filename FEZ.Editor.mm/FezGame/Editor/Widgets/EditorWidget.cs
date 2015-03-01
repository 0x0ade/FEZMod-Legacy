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
    public class EditorWidget : DrawableGameComponent {

        public ILevelEditor LevelEditor { get; set; }
        [ServiceDependency]
        public IMouseStateManager MouseState { get; set; }
        [ServiceDependency]
        public IInputManager InputManager { get; set; }
        [ServiceDependency]
        public IGameService GameService { get; set; }
        [ServiceDependency]
        public IGameStateManager GameState { get; set; }
        [ServiceDependency]
        public IGameCameraManager CameraManager { get; set; }
        [ServiceDependency]
        public IFontManager FontManager { get; set; }
        [ServiceDependency]
        public IContentManagerProvider CMProvider { get; set; }

        public EditorWidget Parent;
        public List<EditorWidget> Widgets = new List<EditorWidget>();

        public Vector2 Position = new Vector2(0f);
        public Vector2 Size = new Vector2(128f);

        public Vector2 Offset {
            get {
                Vector2 offset = new Vector2(0f);
                for (EditorWidget parent = Parent; parent != null; parent = parent.Parent) {
                    offset += parent.Position;
                }
                return offset;
            }
        }

        public Color Background = new Color(0f, 0f, 0f, 0.2f);
        private static Rectangle backgroundBounds = new Rectangle();
        private static Texture2D pixelTexture;

        public EditorWidget(Game game) 
            : base(game) {
            ServiceHelper.InjectServices(this);
        }

        public override void Update(GameTime gameTime) {
            foreach (EditorWidget widget in Widgets) {
                widget.Parent = this;
                widget.LevelEditor = LevelEditor;
                widget.Update(gameTime);
            }
        }

        public override void Draw(GameTime gameTime) {
            DrawBackground(gameTime);

            foreach (EditorWidget widget in Widgets) {
                widget.Parent = this;
                widget.LevelEditor = LevelEditor;
                widget.Draw(gameTime);
            }
        }

        public virtual void Hover(GameTime gameTime) {
        }

        public virtual void Clicked(GameTime gameTime) {
        }

        public virtual void DrawBackground(GameTime gameTime) {
            if (pixelTexture == null) {
                pixelTexture = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                pixelTexture.SetData<Color>(new Color[] { Color.White });
            }

            backgroundBounds.X = (int) Position.X + (int) Offset.X;
            backgroundBounds.Y = (int) Position.Y + (int) Offset.Y;
            backgroundBounds.Width = (int) Size.X;
            backgroundBounds.Height = (int) Size.Y;

            LevelEditor.SpriteBatch.Draw(pixelTexture, backgroundBounds, Background);
        }

    }
}

