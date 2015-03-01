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

        public InfoWidget(Game game) 
            : base(game) {
        }

        public override void Draw(GameTime gameTime) {
            Viewport viewport = GraphicsDevice.Viewport;
            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);

            SpriteFont font = FontManager.Small;
            float fontScale = 1.5f * viewScale;

            string[] metadata = new string[] {
                "Build Date " + LevelEditor.BuildDate,
                "Level: " + (LevelManager.Name ?? "(none)"),
                "Trile Set: " + (LevelManager.TrileSet != null ? LevelManager.TrileSet.Name : "(none)"),
                "Hovered Trile ID: " + (LevelEditor.HoveredTrile != null ? LevelEditor.HoveredTrile.TrileId.ToString() : "(none)"),
                "Hovered Trile: " + (LevelEditor.HoveredTrile != null ? (LevelEditor.HoveredTrile.Trile.Name + " (" + LevelEditor.HoveredTrile.Emplacement.X + ", " + LevelEditor.HoveredTrile.Emplacement.Y + ", " + LevelEditor.HoveredTrile.Emplacement.Z + ")") : "(none)"),
                "Current Trile ID: " + LevelEditor.TrileId,
                "Current Trile: " + (LevelManager.TrileSet != null && LevelManager.TrileSet.Triles.ContainsKey(LevelEditor.TrileId) ? LevelManager.TrileSet.Triles[LevelEditor.TrileId].Name : "(none)"),
                "Current View: " + CameraManager.Viewpoint,
            };

            float lineHeight = font.MeasureString(metadata[0]).Y;
            float lineWidthMax = 0f;
            for (int i = 0; i < metadata.Length; i++) {
                float lineWidth = font.MeasureString(metadata[i]).X;
                if (lineWidth > lineWidthMax) {
                    lineWidthMax = lineWidth;
                }
            }

            Size.X = lineWidthMax * fontScale + lineHeight * 0.5f;
            Size.Y = lineHeight * (metadata.Length + 0.5f);

            DrawBackground();

            for (int i = 0; i < metadata.Length; i++) {
                LevelEditor.GTR.DrawShadowedText(LevelEditor.SpriteBatch, font, metadata[i], new Vector2(Position.X, Position.Y + i * lineHeight), Color.White, fontScale);
            }
        }

    }
}

