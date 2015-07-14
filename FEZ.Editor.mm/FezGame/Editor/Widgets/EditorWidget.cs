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

        protected Color PrevDefaultForeground = LevelEditorOptions.DefaultForeground;
        protected Color PrevDefaultBackground = LevelEditorOptions.DefaultBackground;

        public EditorWidget Parent;
        public List<EditorWidget> Widgets = new List<EditorWidget>();
        public bool ShowChildren = true;
        public bool ClipChildren = false;

        public Vector2 Position = new Vector2(0f);
        public Vector2 Size = new Vector2(128f);

        public bool UpdateBounds = true;

        public Vector2 Offset {
            get {
                Vector2 offset = new Vector2(0f);
                for (EditorWidget parent = Parent; parent != null; parent = parent.Parent) {
                    offset += parent.Position;
                }
                return offset;
            }
        }

        public bool InView {
            get {
                if (!GraphicsDevice.Viewport.Bounds.Intersects(backgroundBounds)) {
                    return false;
                }
                if (this is WindowHeaderWidget || ParentAs<WindowHeaderWidget>() != null) {
                    return true;
                }
                if (Parent != null && Parent.GetType() == typeof(ContainerWidget) && !Parent.backgroundBounds.Intersects(backgroundBounds)) {
                    return false;
                }
                return true;
            }
        }

        protected Color foreground_ = new Color(0f, 0f, 0f, 0f);
        public Color Foreground {
            get {
                Color foreground = foreground_;
                if (Parent != null && foreground.A == 0) {
                    foreground = Parent.Foreground;
                }
                if (foreground.A == 0) {
                    return LevelEditorOptions.DefaultForeground;
                }
                return foreground;
            }
            set {
                foreground_ = value;
            }
        }
        public Color Background = LevelEditorOptions.DefaultBackground;
        protected Rectangle backgroundBounds = new Rectangle();
        protected bool ScissorTestEnablePrev;
        protected Rectangle ScissorRectanglePrev;
        protected readonly static RasterizerState ScissorRasterizerState = new RasterizerState {
            CullMode = CullMode.CullCounterClockwiseFace,
            ScissorTestEnable = true
        };
        protected static Texture2D pixelTexture;

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

            if (!InView) {
                return;
            }

            if (!ShowChildren) {
                return;
            }

            bool clippingChildren = ClipChildren;

            if (clippingChildren) {
                StartClipping();
            }

            foreach (EditorWidget widget in Widgets) {
                widget.Parent = this;
                widget.LevelEditor = LevelEditor;
                widget.Draw(gameTime);
            }

            if (clippingChildren) {
                StopClipping();
            }
        }

        public virtual void Click(GameTime gameTime, int mb) {
        }
        public virtual void Hover(GameTime gameTime) {
            if (ParentAs<ContainerWidget>() != null && ParentAs<ContainerWidget>().Hovered > 0f) {
                ParentAs<ContainerWidget>().Hover(gameTime);
            }
        }
        public virtual void Scroll(GameTime gameTime, int turn) {
            if (ParentAs<ContainerWidget>() != null) {
                ParentAs<ContainerWidget>().ScrollMomentum -= turn * 128f;
            }
        }
        public virtual void Dragging(GameTime gameTime, MouseButtonStates state) {
        }
        public virtual void Unfocus(GameTime gameTime) {
        }
        public virtual void TextInput(char c) {
        }

        public virtual void DrawBackground(GameTime gameTime) {
            backgroundBounds.X = (int) (Position.X + Offset.X);
            backgroundBounds.Y = (int) (Position.Y + Offset.Y);
            backgroundBounds.Width = (int) Size.X;
            backgroundBounds.Height = (int) Size.Y;

            if (!InView) {
                return;
            }

            if (pixelTexture == null) {
                pixelTexture = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                pixelTexture.SetData<Color>(new Color[] { Color.White });
            }

            LevelEditor.SpriteBatch.Draw(pixelTexture, backgroundBounds, Background);
        }

        public virtual void StartClipping() {
            if (ParentAs<ContainerWidget>() != null && ParentAs<ContainerWidget>().ClipChildren) {
                return;
            }

            LevelEditor.SpriteBatch.End();

            ScissorTestEnablePrev = GraphicsDevice.RasterizerState.ScissorTestEnable;
            ScissorRectanglePrev = GraphicsDevice.ScissorRectangle;

            //LevelEditor.SpriteBatch.BeginPoint();
            LevelEditor.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, ScissorRasterizerState);
            GraphicsDevice.ScissorRectangle = backgroundBounds;
        }

        public virtual void StopClipping() {
            if (ParentAs<ContainerWidget>() != null && ParentAs<ContainerWidget>().ClipChildren) {
                return;
            }

            LevelEditor.SpriteBatch.End();

            if (ScissorTestEnablePrev) {
                LevelEditor.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, ScissorRasterizerState);
            } else {
                LevelEditor.SpriteBatch.BeginPoint();
            }
            GraphicsDevice.ScissorRectangle = ScissorRectanglePrev;
        }

        public virtual void UpdateTheme() {
            foreach (EditorWidget widget in Widgets) {
                widget.UpdateTheme();
            }

            if (foreground_.A != 0 ||
                PrevDefaultBackground.R != Background.R || PrevDefaultBackground.G != Background.G || PrevDefaultBackground.B != Background.B) {
                return;
            }

            foreground_.R = LevelEditorOptions.DefaultForeground.R;
            foreground_.G = LevelEditorOptions.DefaultForeground.G;
            foreground_.B = LevelEditorOptions.DefaultForeground.B;

            Background.R = LevelEditorOptions.DefaultBackground.R;
            Background.G = LevelEditorOptions.DefaultBackground.G;
            Background.B = LevelEditorOptions.DefaultBackground.B;

            PrevDefaultForeground = LevelEditorOptions.DefaultForeground;
            PrevDefaultBackground = LevelEditorOptions.DefaultBackground;
        }

        public virtual void Refresh() {
            foreach (EditorWidget widget in Widgets) {
                widget.Refresh();
            }
        }

        public T ParentAs<T>(bool exact = false) where T : EditorWidget {
            EditorWidget parent = Parent;
            while (parent != null && (!(parent is T) || (exact && parent.GetType() != typeof(T)))) {
                parent = parent.Parent;
            }
            return (T) parent;
        }

    }
}

