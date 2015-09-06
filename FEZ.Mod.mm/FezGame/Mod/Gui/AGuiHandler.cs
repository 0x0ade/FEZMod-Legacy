using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FezEngine.Components;
using System.Collections.Generic;
using FezEngine.Tools;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezGame.Services;
using FezEngine.Structure.Input;

namespace FezGame.Mod.Gui {
    public abstract class AGuiHandler : DrawableGameComponent, IGuiHandler {

        [ServiceDependency]
        public IMouseStateManager MouseState { get; set; }
        [ServiceDependency]
        public IInputManager InputManager { get; set; }
        [ServiceDependency]
        public IGameService GameService { get; set; }
        [ServiceDependency]
        public IGameStateManager GameState { get; set; }
        [ServiceDependency]
        public IFontManager FontManager { get; set; }
        [ServiceDependency]
        public IContentManagerProvider CMProvider { get; set; }

        public SpriteBatch SpriteBatch { get; set; }
        public GlyphTextRenderer GTR { get; set; }

        public List<GuiWidget> Widgets { get; set; }
        public List<Action> Scheduled { get; set; }

        public Color DefaultForeground { get; set; }
        public Color DefaultBackground { get; set; }

        protected float SinceMouseMoved = 3f;
        protected bool CursorHovering = false;
        protected Texture2D GrabbedCursor;
        protected Texture2D CanClickCursor;
        protected Texture2D ClickedCursor;
        protected Texture2D PointerCursor;

        protected GuiWidget DraggingWidget;
        protected GuiWidget FocusedWidget;

        public AGuiHandler(Game game)
            : base(game) {
            UpdateOrder = -10;
            DrawOrder = 4000;

            DefaultBackground = new Color(0f, 0f, 0f, 0.75f);
            DefaultForeground = Color.White;
        }

        public override void Initialize() {
            base.Initialize();

            Scheduled = new List<Action>();

            Game.Window.TextInput += delegate(Object sender, TextInputEventArgs e) {
                if (FocusedWidget != null) {
                    FocusedWidget.TextInput(e.Character);
                }
            };

            Widgets = new List<GuiWidget>();
        }

        protected override void LoadContent() {
            base.LoadContent();

            GTR = new GlyphTextRenderer(Game);

            PointerCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_POINTER");
            CanClickCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_CLICKER_A");
            ClickedCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_CLICKER_B");
            GrabbedCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_GRABBER");

            SpriteBatch = new SpriteBatch(GraphicsDevice);
        }

        public override void Update(GameTime gameTime) {
            while (Scheduled.Count > 0) {
                Scheduled[0]();
                Scheduled.RemoveAt(0);
            }

            if (GraphicsDevice == null || SpriteBatch == null) {
                return;
            }

            SinceMouseMoved += (float) gameTime.ElapsedGameTime.TotalSeconds;
            if (MouseState.Movement.X != 0 || MouseState.Movement.Y != 0) {
                SinceMouseMoved = 0f;
            }
            CursorHovering = false;

            bool cursorInMenu = UpdateWidgets(gameTime, Widgets, true);

            if (DraggingWidget != null && (MouseState.LeftButton.State == MouseButtonStates.Dragging || MouseState.LeftButton.State == MouseButtonStates.DragEnded)) {
                DraggingWidget.Dragging(gameTime, MouseState.LeftButton.State);
                cursorInMenu = true;

                if (MouseState.LeftButton.State == MouseButtonStates.DragEnded) {
                    DraggingWidget = null;
                }
            }

            if (cursorInMenu) {
                CursorHovering = true;
                return;
            }

            if (MouseState.LeftButton.State == MouseButtonStates.Clicked) {
                if (FocusedWidget != null) {
                    FocusedWidget.Unfocus(gameTime);
                }
                FocusedWidget = null;
            }
        }

        public override void Draw(GameTime gameTime) {
            if (!FEZMod.Preloaded || GraphicsDevice == null || SpriteBatch == null) {
                return;
            }

            Viewport viewport = GraphicsDevice.Viewport;
            float viewScale = GraphicsDevice.GetViewScale();

            float cursorScale = viewScale * 2f;
            Point cursorPosition = MouseState.PositionInViewport();
            Texture2D cursor = MouseState.LeftButton.State == MouseButtonStates.Dragging || MouseState.RightButton.State == MouseButtonStates.Dragging ? GrabbedCursor : (CursorHovering ? (MouseState.LeftButton.State == MouseButtonStates.Down || MouseState.RightButton.State == MouseButtonStates.Down ? ClickedCursor : CanClickCursor) : PointerCursor);
            if (cursor == CanClickCursor || cursor == ClickedCursor) {
                cursorPosition.X -= 8;
            }

            GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
            SpriteBatch.BeginPoint();

            foreach (GuiWidget widget in Widgets) {
                widget.GuiHandler = this;
                widget.Draw(gameTime);
            }

            SpriteBatch.Draw(cursor, 
                new Vector2(
                    (float) cursorPosition.X - cursorScale * 11.5f,
                    (float) cursorPosition.Y - cursorScale * 8.5f
                ), new Rectangle?(),
                new Color(1f, 1f, 1f, FezMath.Saturate((float) (1.0 - ((double) SinceMouseMoved - 2.0)))),
                0.0f,
                Vector2.Zero,
                cursorScale,
                SpriteEffects.None,
                0.0f);

            SpriteBatch.End();
        }

        protected bool UpdateWidgets(GameTime gameTime, List<GuiWidget> widgets, Boolean update) {
            bool cursorOnWidget = false;
            for (int i = widgets.Count - 1; i >= 0; i--) {
                GuiWidget widget = widgets[i];
                widget.GuiHandler = this;
                if (update) {
                    widget.PreUpdate();
                    widget.Update(gameTime);
                }
                bool cursorOnChild = cursorOnWidget;
                if (widget.ShowChildren) {
                    cursorOnChild = cursorOnWidget || UpdateWidgets(gameTime, widget.Widgets, false);
                }
                if (widget.InView && (widget.Position.X + widget.Offset.X <= MouseState.Position.X && MouseState.Position.X <= widget.Position.X + widget.Offset.X + widget.Size.X &&
                    widget.Position.Y + widget.Offset.Y <= MouseState.Position.Y && MouseState.Position.Y <= widget.Position.Y + widget.Offset.Y + widget.Size.Y)) {
                    cursorOnWidget = true;
                    widget.Hover(gameTime);
                    if (!cursorOnChild && MouseState.LeftButton.State == MouseButtonStates.Clicked) {
                        widget.Click(gameTime, 1);
                        if (FocusedWidget != null) {
                            FocusedWidget.Unfocus(gameTime);
                        }
                        FocusedWidget = widget;
                    }
                    if (!cursorOnChild && MouseState.RightButton.State == MouseButtonStates.Clicked) {
                        widget.Click(gameTime, 3);
                    }
                    if (!cursorOnChild && MouseState.LeftButton.State == MouseButtonStates.DragStarted) {
                        if (DraggingWidget != null) {
                            DraggingWidget.Dragging(gameTime, MouseButtonStates.DragEnded);
                        }
                        DraggingWidget = widget;
                        DraggingWidget.Dragging(gameTime, MouseButtonStates.DragStarted);
                    }
                    widget.Scroll(gameTime, MouseState.WheelTurns);
                }
                cursorOnWidget = cursorOnWidget || cursorOnChild;
            }
            return cursorOnWidget;
        }

    }
}

