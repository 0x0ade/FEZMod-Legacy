using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FezGame.Mod.Gui {
    public class TrilePickerWidget : GuiWidget {

        [ServiceDependency]
        public IGameLevelManager LevelManager { get; set; }

        protected TrileSet TrileSetOld;

        public float ScrollMomentum = 0f;
        public float ScrollOffset = 0f;
        public float ScrollOffsetPreDrag = 0f;
        public float InnerSize = 0f;
        
        public GuiWidget[] PermanentWidgets;
        public TextFieldWidget SearchField;

        protected Rectangle tmpRect = new Rectangle();
        
        protected bool large = false;
        public bool Large {
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

        public Texture2D TrileAtlas { get; set; }

        public TrilePickerWidget(Game game) 
            : base(game) {
            PermanentWidgets = new GuiWidget[] {
                new TrileAllButtonWidget(game),
                new ButtonWidget(game, "Search:"),
                SearchField = new TextFieldWidget(game) {
                    OnInput = Search
                },
                new ButtonWidget(game, " X") {
                    Background = Color.Red,
                    Action = delegate() {
                        ((TextFieldWidget) PermanentWidgets[2]).Text = String.Empty;
                        Search();
                    },
                    UpdateBounds = false,
                    Size = new Vector2(24f, 24f)
                }
            };
            Widgets.AddRange(PermanentWidgets);
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
            float maxSize = large ? Size.Y : Size.X;
            if (ScrollOffset > InnerSize - maxSize) {
                ScrollOffset = InnerSize - maxSize;
            }
            if (InnerSize <= maxSize) {
                ScrollOffset = 0f;
            }

            if (UpdateBounds) {
                Size.X = GraphicsDevice.Viewport.Width;
                Size.Y = (large ? 14f : 1f) * 36f ;
            }
            
            TrileAllButtonWidget allButton = (TrileAllButtonWidget) PermanentWidgets[0];
            ButtonWidget searchLabel = (ButtonWidget) PermanentWidgets[1];
            TextFieldWidget searchField = (TextFieldWidget) PermanentWidgets[2];
            ButtonWidget searchReset = (ButtonWidget) PermanentWidgets[3];
            
            allButton.Update(gameTime);
            allButton.Position = Vector2.Zero;
            if (!large) {
                for (int i = 1; i < PermanentWidgets.Length; i++) {
                    PermanentWidgets[i].Visible = false;
                }
                
                InnerSize = allButton.Size.X + 4f;
                for (int i = 0; i < Widgets.Count - PermanentWidgets.Length; i++) {
                    if (!Widgets[i].Visible) {
                        continue;
                    }
                    Widgets[i].Update(gameTime);

                    Widgets[i].Position.X = InnerSize - ScrollOffset;
                    Widgets[i].Position.Y = 0;
                    ((TrileButtonWidget) Widgets[i]).Tooltip.Background = GuiHandler.DefaultBackground;

                    InnerSize += Widgets[i].Size.X + 4f;
                }
            } else {
                for (int i = 1; i < PermanentWidgets.Length; i++) {
                    PermanentWidgets[i].Visible = true;
                    PermanentWidgets[i].Update(gameTime);
                }
                
                allButton.Position.Y = 12f;
                searchLabel.Background.A = 0;
                searchLabel.Position.X = allButton.Size.X + 4f;
                searchLabel.Position.Y = 0f;
                searchField.Position = searchLabel.Position;
                searchField.Position.Y += searchLabel.Size.Y + 4f;
                searchField.Size.X = Size.X - allButton.Size.X - 4f - 24f;
                searchReset.Position = searchField.Position;
                searchReset.Position.X += searchField.Size.X;
                
                float xpadding = 64f;
                float xoffs = xpadding;
                float yoffs = allButton.Size.Y + 24f;
                for (int i = 0; i < Widgets.Count - PermanentWidgets.Length; i++) {
                    if (!Widgets[i].Visible) {
                        continue;
                    }
                    Widgets[i].Update(gameTime);

                    Widgets[i].Position.X = xoffs;
                    Widgets[i].Position.Y = yoffs - ScrollOffset;
                    Widgets[i].ShowChildren = true;
                    ((TrileButtonWidget) Widgets[i]).Tooltip.Background.A = 0;
                    ((TrileButtonWidget) Widgets[i]).Tooltip.Position.Y = Widgets[i].Size.Y;
                    
                    xoffs += Widgets[i].Size.X + xpadding * 2f;
                    if (Size.X <= xoffs) {
                        xoffs = xpadding;
                        yoffs += Widgets[i].Size.Y + 24f;
                    }
                }
                InnerSize = yoffs + Widgets[0].Size.Y + 24f;
            }
        }
        
        public override void Draw(GameTime gameTime) {
            if (!large) {
                base.Draw(gameTime);
                return;
            }
            
            DrawBackground(gameTime);

            if (!InView || !ShowChildren) {
                return;
            }

            bool clippingChildren = ClipChildren;

            if (clippingChildren) {
                StartClipping();
            }

            for (int i = 0; i < Widgets.Count - PermanentWidgets.Length; i++) {
                GuiWidget widget = Widgets[i];
                widget.Parent = this;
                widget.GuiHandler = GuiHandler;
                widget.Draw(gameTime);
            }
            
            if (clippingChildren) {
                StopClipping();
            }
            
            tmpRect.X = (int) Position.X;
            tmpRect.Y = (int) Position.Y;
            tmpRect.Width = (int) Size.X;
            tmpRect.Height = 52;
            GuiHandler.SpriteBatch.Draw(pixelTexture, tmpRect, Background);
            
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

        public void UpdateWidgets() {
            Widgets.Clear();
            ScrollOffset = 0f;

            //WARNING: It is not performant as it reads the orig atlas from the GPU / VRAM, modifies it on the CPU and then pushes it back to VRAM.
            //TODO: Learn how to use FBOs in MonoDevelop / XNA.
            if (TrileAtlas != null) {
                TrileAtlas.Dispose();
            }
            TrileAtlas = new Texture2D(GraphicsDevice, LevelManager.TrileSet.TextureAtlas.Width, LevelManager.TrileSet.TextureAtlas.Height);
            Color[] trileAtlasData = new Color[TrileAtlas.Width * TrileAtlas.Height];
            LevelManager.TrileSet.TextureAtlas.GetData(trileAtlasData);
            for (int i = 0; i < trileAtlasData.Length; i++) {
                trileAtlasData[i].A = 255;
            }
            TrileAtlas.SetData(trileAtlasData);

            foreach (Trile trile in LevelManager.TrileSet.Triles.Values) {
                Widgets.Add(new TrileButtonWidget(Game, trile) {
                    TrileAtlas = TrileAtlas
                });
            }
            
            Widgets.AddRange(PermanentWidgets);
        }
        
        public void Search() {
            ScrollMomentum = 0f;
            ScrollOffset = 0f;
            ScrollOffsetPreDrag = 0f;
            
            string query = ((TextFieldWidget) PermanentWidgets[2]).Text;
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
            
            for (int i = 0; i < Widgets.Count - PermanentWidgets.Length; i++) {
                TrileButtonWidget widget = (TrileButtonWidget) Widgets[i];
                string name = widget.Trile.Name.ToLower();
                bool contains = false;
                
                for (int ii = 0; ii < items.Length; ii++) {
                    if (name.Contains(items[ii])) {
                        contains = true;
                        break;
                    }
                }
                
                widget.Visible = contains;
            }
        }
        
        public override void Scroll(GameTime gameTime, int turn) {
            ScrollMomentum -= turn * (large ? 128f : 64f);
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

