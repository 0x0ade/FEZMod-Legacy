using System;
using Microsoft.Xna.Framework;
using FezGame.Components;
using FezEngine.Structure.Input;

namespace FezGame.Mod.Gui {
    public class ThumbnailCreatorWidget : GuiWidget {

        protected Vector2 PreDrag = new Vector2(-1337, -1337);

        protected int OldSize;

        public ThumbnailCreatorWidget(Game game) 
            : base(game) {
        }

        public override void Update(GameTime gameTime) {
            if (UpdateBounds) {
                Size.X = ((ILevelEditor) GuiHandler).ThumbnailSize + 4f;
                Size.Y = ((ILevelEditor) GuiHandler).ThumbnailSize + 4f + 24f;
                Background.A = 0;
            }

            if (((ILevelEditor) GuiHandler).ThumbnailSize != OldSize) {
                Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (Size.X / 2);
                Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (Size.Y / 2) + 12 * CameraManager.PixelsPerTrixel;
                OldSize = ((ILevelEditor) GuiHandler).ThumbnailSize;
                UpdateWidgets();
            }

            for (int i = 0; i < Widgets.Count; i++) {
                Widgets[i].Update(gameTime);
            }
        }

        public override void DrawBackground(GameTime gameTime) {
            base.DrawBackground(gameTime);

            if (!InView) {
                return;
            }

            GuiHandler.SpriteBatch.Draw(pixelTexture, new Rectangle((int) Position.X, (int) Position.Y, 2, ((ILevelEditor) GuiHandler).ThumbnailSize + 4), GuiHandler.DefaultBackground);
            GuiHandler.SpriteBatch.Draw(pixelTexture, new Rectangle((int) Position.X + ((ILevelEditor) GuiHandler).ThumbnailSize + 2, (int) Position.Y, 2, ((ILevelEditor) GuiHandler).ThumbnailSize + 4), GuiHandler.DefaultBackground);
        }

        public void UpdateWidgets() {
            Widgets.Clear();

            WindowHeaderWidget header;
            Widgets.Add(header = new WindowHeaderWidget(Game) {
                Label = ""
            });
            header.Widgets.Add(new ButtonWidget(Game, " -", delegate() {
                ((ILevelEditor) GuiHandler).ThumbnailSize = Math.Max(128, ((ILevelEditor) GuiHandler).ThumbnailSize / 2);
            }) {
                Background = Color.Blue,
                UpdateBounds = false,
                Size = new Vector2(24f, 24f)
            });
            header.Widgets.Add(new ButtonWidget(Game, " +", delegate() {
                ((ILevelEditor) GuiHandler).ThumbnailSize *= 2;
                if (((ILevelEditor) GuiHandler).ThumbnailSize >= GraphicsDevice.Viewport.Width || ((ILevelEditor) GuiHandler).ThumbnailSize >= GraphicsDevice.Viewport.Height) {
                    ((ILevelEditor) GuiHandler).ThumbnailSize /= 2;
                }
            }) {
                Background = Color.Green,
                UpdateBounds = false,
                Size = new Vector2(24f, 24f)
            });

            Widgets.Add(new ButtonWidget(Game, "CREATE", delegate() {
                ((ILevelEditor) GuiHandler).ThumbnailX = (int) Position.X + 2;
                ((ILevelEditor) GuiHandler).ThumbnailY = (int) Position.Y + 2;
                FEZMod.CreatingThumbnail = true;
                ((ILevelEditor) GuiHandler).ThumbnailScheduled = true;
                header.CloseButtonWidget.Action();
            }) {
                Background = new Color(0f, 0.125f, 0f, 1f),
                Size = new Vector2(Size.X, 24f),
                UpdateBounds = false,
                LabelCentered = true,
                Position = new Vector2(0f, Size.Y - 24f)
            });
        }

        public override void Scroll(GameTime gameTime, int turn) {
            CameraManager.PixelsPerTrixel = Math.Max(1f, CameraManager.PixelsPerTrixel + 0.5f * turn);
        }

        public override void Dragging(GameTime gameTime, MouseButtonStates state) {
            if (state == MouseButtonStates.DragEnded || MouseState.LeftButton.DragState == null || MouseState.LeftButton.DragState.Movement == null) {
                return;
            }

            if (state == MouseButtonStates.DragStarted || (PreDrag.X == PreDrag.Y && PreDrag.X == -1337)) {
                PreDrag = (Parent ?? this).Position;
            }

            Position.X = PreDrag.X + MouseState.LeftButton.DragState.Movement.X;
            Position.Y = PreDrag.Y + MouseState.LeftButton.DragState.Movement.Y;
        }

    }
}

