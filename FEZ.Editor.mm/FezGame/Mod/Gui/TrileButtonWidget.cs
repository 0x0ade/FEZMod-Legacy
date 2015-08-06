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

namespace FezGame.Mod.Gui {
    public class TrileButtonWidget : ButtonWidget {

        [ServiceDependency]
        public IGameLevelManager LevelManager { get; set; }
        [ServiceDependency]
        public IPlayerManager PlayerManager { get; set; }

        public Trile Trile;
        public Texture2D TrileAtlas { get; set; }

        public ButtonWidget Tooltip;

        public TrileButtonWidget(Game game) 
            : this(game, null) {
        }

        public TrileButtonWidget(Game game, Trile trile) 
            : base(game) {
            Trile = trile;
            Widgets.Add(Tooltip = new ButtonWidget(game));
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            if (Trile == null) {
                return;
            }

            if (UpdateBounds) {
                Size.X = 32f;
                Size.Y = 32f;

                Tooltip.UpdateBounds = true;

                Tooltip.Label = Trile.Name;

                Tooltip.Position.X = -Tooltip.Size.X / 2f + Size.X / 2f;
                Tooltip.Position.Y = -Tooltip.Size.Y;
            }
        }

        public override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

            if (Trile == null || !InView || TrileAtlas == null) {
                return;
            }

            GuiHandler.SpriteBatch.Draw(TrileAtlas, new Rectangle(
                (int) (Position.X + Offset.X),
                (int) (Position.Y + Offset.Y),
                32, 32
                ), new Rectangle(
                (int) Math.Ceiling(((double)Trile.AtlasOffset.X) * ((double)TrileAtlas.Width)) + 1,
                (int) Math.Ceiling(((double)Trile.AtlasOffset.Y) * ((double)TrileAtlas.Height)) + 1,
                16, 16
                ), Color.White);
        }

        public override void Click(GameTime gameTime, int mb) {
            if (Trile == null) {
                return;
            }

            if (mb == 1) {
                ((ILevelEditor) GuiHandler).TrileId = Trile.Id;
            } else if (mb == 3) {
                GuiHandler.Scheduled.Add(delegate() {
                    ContainerWidget window;
                    GuiHandler.Widgets.Add(window = new ContainerWidget(Game));
                    window.Size.X = 256f;
                    window.Size.Y = 144f;
                    window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int)(window.Size.X / 2);
                    window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int)(window.Size.Y / 2);
                    window.Label = "Place trile";
                    WindowHeaderWidget windowHeader;
                    window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

                    ButtonWidget windowLabelID;
                    window.Widgets.Add(windowLabelID = new ButtonWidget(Game, "ID:"));
                    windowLabelID.Background.A = 0;
                    windowLabelID.Size.X = 96f;
                    windowLabelID.Size.Y = 24f;
                    windowLabelID.UpdateBounds = false;
                    windowLabelID.LabelCentered = false;
                    windowLabelID.Position.X = 0f;
                    windowLabelID.Position.Y = 0f;
                    TextFieldWidget windowFieldID;
                    window.Widgets.Add(windowFieldID = new TextFieldWidget(Game));
                    windowFieldID.Text = Trile.Id.ToString();
                    windowFieldID.Size.X = window.Size.X - windowLabelID.Size.X;
                    windowFieldID.Size.Y = 24f;
                    windowFieldID.UpdateBounds = false;
                    windowFieldID.Position.X = windowLabelID.Size.X;
                    windowFieldID.Position.Y = windowLabelID.Position.Y;

                    ButtonWidget windowLabelX;
                    window.Widgets.Add(windowLabelX = new ButtonWidget(Game, "X:"));
                    windowLabelX.Background.A = 0;
                    windowLabelX.Size.X = 96f;
                    windowLabelX.Size.Y = 24f;
                    windowLabelX.UpdateBounds = false;
                    windowLabelX.LabelCentered = false;
                    windowLabelX.Position.X = 0f;
                    windowLabelX.Position.Y = 24f;
                    TextFieldWidget windowFieldX;
                    window.Widgets.Add(windowFieldX = new TextFieldWidget(Game));
                    windowFieldX.Text = ((int) PlayerManager.Position.X).ToString();
                    windowFieldX.Size.X = window.Size.X - windowLabelX.Size.X;
                    windowFieldX.Size.Y = 24f;
                    windowFieldX.UpdateBounds = false;
                    windowFieldX.Position.X = windowLabelX.Size.X;
                    windowFieldX.Position.Y = windowLabelX.Position.Y;

                    ButtonWidget windowLabelY;
                    window.Widgets.Add(windowLabelY = new ButtonWidget(Game, "Y:"));
                    windowLabelY.Background.A = 0;
                    windowLabelY.Size.X = 96f;
                    windowLabelY.Size.Y = 24f;
                    windowLabelY.UpdateBounds = false;
                    windowLabelY.LabelCentered = false;
                    windowLabelY.Position.X = 0f;
                    windowLabelY.Position.Y = 48f;
                    TextFieldWidget windowFieldY;
                    window.Widgets.Add(windowFieldY = new TextFieldWidget(Game));
                    windowFieldY.Text = ((int) PlayerManager.Position.Y).ToString();
                    windowFieldY.Size.X = window.Size.X - windowLabelY.Size.X;
                    windowFieldY.Size.Y = 24f;
                    windowFieldY.UpdateBounds = false;
                    windowFieldY.Position.X = windowLabelY.Size.X;
                    windowFieldY.Position.Y = windowLabelY.Position.Y;

                    ButtonWidget windowLabelZ;
                    window.Widgets.Add(windowLabelZ = new ButtonWidget(Game, "Z:"));
                    windowLabelZ.Background.A = 0;
                    windowLabelZ.Size.X = 96f;
                    windowLabelZ.Size.Y = 24f;
                    windowLabelZ.UpdateBounds = false;
                    windowLabelZ.LabelCentered = false;
                    windowLabelZ.Position.X = 0f;
                    windowLabelZ.Position.Y = 72f;
                    TextFieldWidget windowFieldZ;
                    window.Widgets.Add(windowFieldZ = new TextFieldWidget(Game));
                    windowFieldZ.Text = ((int) PlayerManager.Position.Z).ToString();
                    windowFieldZ.Size.X = window.Size.X - windowLabelZ.Size.X;
                    windowFieldZ.Size.Y = 24f;
                    windowFieldZ.UpdateBounds = false;
                    windowFieldZ.Position.X = windowLabelZ.Size.X;
                    windowFieldZ.Position.Y = windowLabelZ.Position.Y;

                    ButtonWidget windowLabelFace;
                    window.Widgets.Add(windowLabelFace = new ButtonWidget(Game, "Face:"));
                    windowLabelFace.Background.A = 0;
                    windowLabelFace.Size.X = 96f;
                    windowLabelFace.Size.Y = 24f;
                    windowLabelFace.UpdateBounds = false;
                    windowLabelFace.LabelCentered = false;
                    windowLabelFace.Position.X = 0f;
                    windowLabelFace.Position.Y = 96f;
                    TextFieldWidget windowFieldFace;
                    window.Widgets.Add(windowFieldFace = new TextFieldWidget(Game));
                    windowFieldFace.Text = LevelManager.StartingPosition.Face.ToString();
                    windowFieldFace.Size.X = window.Size.X - windowLabelFace.Size.X;
                    windowFieldFace.Size.Y = 24f;
                    windowFieldFace.UpdateBounds = false;
                    windowFieldFace.Position.X = windowLabelFace.Size.X;
                    windowFieldFace.Position.Y = windowLabelFace.Position.Y;
                    windowFieldFace.Fill(Enum.GetNames(typeof(FaceOrientation)));

                    ButtonWidget windowButtonCreate;
                    window.Widgets.Add(windowButtonCreate = new ButtonWidget(Game, "CREATE", delegate() {
                        int trileId = int.Parse(windowFieldID.Text);
                        TrileInstance trile = ((ILevelEditor) GuiHandler).CreateNewTrile(trileId,new TrileEmplacement(
                                              int.Parse(windowFieldX.Text),
                                              int.Parse(windowFieldY.Text),
                                              int.Parse(windowFieldZ.Text)
                        ));
                        trile.Phi = ((FaceOrientation) Enum.Parse(typeof(FaceOrientation), windowFieldFace.Text)).ToPhi();
                        ((ILevelEditor) GuiHandler).AddTrile(trile);
                        windowHeader.CloseButtonWidget.Action();
                    }));
                    windowButtonCreate.Size.X = window.Size.X;
                    windowButtonCreate.Size.Y = 24f;
                    windowButtonCreate.UpdateBounds = false;
                    windowButtonCreate.LabelCentered = true;
                    windowButtonCreate.Position.X = 0f;
                    windowButtonCreate.Position.Y = window.Size.Y - windowButtonCreate.Size.Y;
                });
            }
        }

        public override void Dragging(GameTime gameTime, MouseButtonStates state) {
            if (Parent != null) {
                Parent.Dragging(gameTime, state);
            } else {
                base.Dragging(gameTime, state);
            }
        }

    }
}

