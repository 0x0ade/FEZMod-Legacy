using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FezGame.Mod.Gui {
    public abstract class AssetPickerWidget : GuiWidget {

        public float ScrollMomentum = 0f;
        public float ScrollOffset = 0f;
        public float ScrollOffsetPreDrag = 0f;
        public float InnerSize = 0f;
        
        public bool NoAlpha = false; //TODO: don't apply on tooltips
        
        public GuiWidget[] PermanentWidgets;
        public AllButtonWidget AllButton;
        public ButtonWidget SearchLabel;
        public TextFieldWidget SearchField;
        public ButtonWidget SearchReset;

        protected Rectangle tmpRect = new Rectangle();
        
        protected bool large = false;
        public virtual bool Large {
            get {
                return large;
            }
            set {
                if (large != value) {
                    ScrollMomentum = 0f;
                    ScrollOffset = 0f;
                    ScrollOffsetPreDrag = 0f;
                    
                    ClipChildren = value;
                }
                
                large = value;
            }
        }
        protected float LargeSpacingX = 64f;
        public float LargeRows = 10f;

        public AssetPickerWidget(Game game) 
            : base(game) {
            PermanentWidgets = new GuiWidget[] {
                AllButton = new AllButtonWidget(game),
                SearchLabel = new ButtonWidget(game, "Search:"),
                SearchField = new TextFieldWidget(game) {
                    OnInput = Search
                },
                SearchReset = new ButtonWidget(game, " X") {
                    Background = Color.Red,
                    Action = delegate() {
                        SearchField.Text = String.Empty;
                        Search();
                    },
                    UpdateBounds = false,
                    Size = new Vector2(24f, 24f)
                }
            };
            Widgets.AddRange(PermanentWidgets);
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            
            ScrollOffset += ScrollMomentum;
            ScrollMomentum *= 0.43f;
            if (ScrollOffset < 0f) {
                ScrollOffset = 0f;
            }
            float maxSize = large ? Size.Y : Size.X;
            if (ScrollOffset > InnerSize - maxSize) {
                ScrollOffset = InnerSize - maxSize;
            }
            if (InnerSize <= maxSize) {
                ScrollOffset = 0f;
            }

            if (UpdateBounds && Parent == null) {
                Size.X = GraphicsDevice.Viewport.Width;
                Size.Y = (large ? LargeRows : 1f) * (Widgets[0].Size.Y + 4f);
            }
            
            AllButton.Update(gameTime);
            AllButton.Position = Vector2.Zero;
            SearchLabel.Visible = SearchField.Visible = SearchReset.Visible = large;
            if (!large) {
                InnerSize = AllButton.Size.X + 4f;
                for (int i = 0; i < Widgets.Count - PermanentWidgets.Length; i++) {
                    AssetButtonWidget widget = (AssetButtonWidget) Widgets[i];
                    if (widget.InView) {
                        widget.Update(gameTime);
                    }

                    widget.Position.X = InnerSize - ScrollOffset;
                    widget.Position.Y = 0;
                    widget.Tooltip.Background = GuiHandler.DefaultBackground;

                    InnerSize += widget.Size.X + 4f;
                }
            } else {
                for (int i = 1; i < PermanentWidgets.Length; i++) {
                    PermanentWidgets[i].Update(gameTime);
                }
                
                AllButton.Position.Y = 12f;
                if (!AllButton.Visible) {
                    AllButton.Size.X = 0f;
                }
                SearchLabel.Background.A = 0;
                SearchLabel.Position.X = AllButton.Size.X + 4f;
                SearchLabel.Position.Y = 0f;
                SearchField.Position = SearchLabel.Position;
                SearchField.Position.Y += SearchLabel.Size.Y + 4f;
                SearchField.Size.X = Size.X - AllButton.Size.X - 4f - 24f;
                SearchReset.Position = SearchField.Position;
                SearchReset.Position.X += SearchField.Size.X;
                
                float xoffs = LargeSpacingX;
                float yoffs = AllButton.Size.Y + 24f;
                for (int i = 0; i < Widgets.Count - PermanentWidgets.Length; i++) {
                    AssetButtonWidget widget = (AssetButtonWidget) Widgets[i];
                    if (widget.InView) {
                        widget.Update(gameTime);
                    }

                    widget.Position.X = xoffs;
                    widget.Position.Y = yoffs - ScrollOffset;
                    widget.ShowChildren = true;
                    widget.Tooltip.Background.A = 0;
                    widget.Tooltip.Position.Y = widget.Size.Y;
                    
                    xoffs += widget.Size.X + LargeSpacingX * 2f;
                    if (Size.X <= (xoffs + widget.Size.X)) {
                        xoffs = LargeSpacingX;
                        yoffs += widget.Size.Y + 24f;
                    }
                }
                InnerSize = yoffs + Widgets[0].Size.Y + 24f;
            }
        }
        
        public override void Draw(GameTime gameTime) {
            DrawBackground(gameTime);

            if (!InView || !ShowChildren) {
                return;
            }

            bool clippingChildren = ClipChildren;

            if (clippingChildren) {
                StartClipping(NoAlpha ? BlendState.Opaque : null);
            } else if (NoAlpha) {
                GuiHandler.SpriteBatch.End();
                GuiHandler.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, null, RasterizerState.CullCounterClockwise);
            }

            CullChildren = true;
            for (int i = 0; i < Widgets.Count - PermanentWidgets.Length; i++) {
                GuiWidget widget = Widgets[i];
                widget.Parent = this;
                widget.GuiHandler = GuiHandler;
                widget.Draw(gameTime);
            }
            
            if (clippingChildren) {
                StopClipping();
            } else if (NoAlpha) {
                GuiHandler.SpriteBatch.End();
                GuiHandler.SpriteBatch.BeginPoint();
            }
            
            if (large) {
                tmpRect.X = (int) Position.X;
                tmpRect.Y = (int) Position.Y;
                tmpRect.Width = (int) Size.X;
                tmpRect.Height = 52;
                GuiHandler.SpriteBatch.Draw(pixelTexture, tmpRect, Background);
            }
            
            CullChildren = false;
            for (int i = 0; i < PermanentWidgets.Length; i++) {
                GuiWidget widget = PermanentWidgets[i];
                widget.Parent = this;
                widget.GuiHandler = GuiHandler;
                widget.Draw(gameTime);
            }
        }

        public override void DrawBackground(GameTime gameTime) {
            if (large) {
                backgroundBounds.X = (int) (Position.X + Offset.X);
                backgroundBounds.Y = (int) (Position.Y + Offset.Y) + 52;
                backgroundBounds.Width = (int) Size.X;
                backgroundBounds.Height = (int) Size.Y;

                if (!InView) {
                    return;
                }

                if (pixelTexture == null) {
                    base.DrawBackground(gameTime);
                } else {
                    GuiHandler.SpriteBatch.Draw(pixelTexture, backgroundBounds, Background * 1.2f);
                }
            } else {
                base.DrawBackground(gameTime);
            }

            if (!InView || InnerSize <= (large ? Size.Y : Size.X)) {
                return;
            }
            
            if (!large) {
                tmpRect.X = backgroundBounds.X + (int) (Size.X * ScrollOffset / InnerSize);
                tmpRect.Y = backgroundBounds.Y + (int) (Size.Y) - 4;
                tmpRect.Width = (int) (Size.X * Size.X / InnerSize);
                tmpRect.Height = 4;
            } else {
                tmpRect.Y = backgroundBounds.Y + (int) (Size.Y * ScrollOffset / InnerSize);
                tmpRect.X = backgroundBounds.X + (int) (Size.X) - 4;
                tmpRect.Height = (int) (Size.Y * Size.Y / InnerSize);
                tmpRect.Width = 4;
            }

            GuiHandler.SpriteBatch.Draw(pixelTexture, tmpRect, new Color(255, 255, 255, Background.A));
        }

        public abstract void UpdateWidgets();
        
        public virtual void Search() {
            ScrollMomentum = 0f;
            ScrollOffset = 0f;
            ScrollOffsetPreDrag = 0f;
            
            string query = SearchField.Text;
            if (string.IsNullOrEmpty(query)) {
                for (int i = 0; i < Widgets.Count - PermanentWidgets.Length; i++) {
                    Widgets[i].Visible = true;
                }
                return;
            }
            query = query.ToLower();
            
            string[] items;
            if (query.Contains(" ")) {
                items = query.Split(' ');
            } else {
                items = new string[] {query};
            }
            
            Search(items);
        }
        
        public abstract void Search(string[] items);
        
        public override void Scroll(GameTime gameTime, int turn) {
            ScrollMomentum -= turn * (large ? 96f : 64f);
        }

        public override void Dragging(GameTime gameTime, MouseButtonStates state) {
            if (state == MouseButtonStates.DragEnded) {
                return;
            }

            if (state == MouseButtonStates.DragStarted) {
                ScrollOffsetPreDrag = ScrollOffset;
            }

            float dir = large ? MouseState.LeftButton.DragState.Movement.Y : MouseState.LeftButton.DragState.Movement.X;
            ScrollOffset = ScrollOffsetPreDrag + dir * 3f;
        }

    }
}

