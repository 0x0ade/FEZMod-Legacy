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
    public class InfoWidget : EditorWidget {

        [ServiceDependency]
        public IGameLevelManager LevelManager { get; set; }
        [ServiceDependency]
        public IPlayerManager PlayerManager { get; set; }

        public SpriteFont Font;

        public InfoWidget(Game game) 
            : base(game) {
            Font = FontManager.Small;
        }

        public override void Update(GameTime gameTime) {
            string[] informations = GetInformations();

            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);

            float lineHeight = Font.MeasureString(informations[0]).Y * 0.5f * viewScale;
            float lineWidthMax = 0f;
            for (int i = 0; i < informations.Length; i++) {
                float lineWidth = Font.MeasureString(informations[i]).X;
                if (lineWidth > lineWidthMax) {
                    lineWidthMax = lineWidth;
                }
            }
            lineWidthMax *= viewScale;

            if (UpdateBounds) {
                Size.X = lineWidthMax + lineHeight;
                Size.Y = lineHeight * (informations.Length + 1f);
            }
        }

        public override void Draw(GameTime gameTime) {
            DrawBackground(gameTime);

            if (!InView) {
                return;
            }

            string[] informations = GetInformations();

            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);
            float lineHeight = Font.MeasureString(informations[0]).Y * 0.5f * viewScale;
            for (int i = 0; i < informations.Length; i++) {
                LevelEditor.GTR.DrawShadowedText(LevelEditor.SpriteBatch, Font, informations[i], new Vector2(Position.X, Position.Y + i * lineHeight) + Offset, Color.White, viewScale);
            }
        }

        public virtual string[] GetInformations() {
            return new string[] {
                "Build Date " + LevelEditor.BuildDate,
                "Level: " + (LevelManager.Name ?? "(none)"),
                "Gomez Position: " + (PlayerManager != null ? (" (" + PlayerManager.Position.X + ", " + PlayerManager.Position.Y + ", " + PlayerManager.Position.Z + ")") : "(none)"),
                "Trile Set: " + (LevelManager.TrileSet != null ? LevelManager.TrileSet.Name : "(none)"),
                "Hovered Trile ID: " + (LevelEditor.HoveredTrile != null ? LevelEditor.HoveredTrile.TrileId.ToString() : "(none)"),
                "Hovered Trile: " + (LevelEditor.HoveredTrile != null ? (LevelEditor.HoveredTrile.Trile.Name + " (" + LevelEditor.HoveredTrile.Emplacement.X + ", " + LevelEditor.HoveredTrile.Emplacement.Y + ", " + LevelEditor.HoveredTrile.Emplacement.Z + ")") : "(none)"),
                "Current Trile ID: " + LevelEditor.TrileId,
                "Current Trile: " + (LevelManager.TrileSet != null && LevelManager.TrileSet.Triles.ContainsKey(LevelEditor.TrileId) ? LevelManager.TrileSet.Triles[LevelEditor.TrileId].Name : "(none)"),
                "Current View: " + CameraManager.Viewpoint
            };
        }

    }
}

