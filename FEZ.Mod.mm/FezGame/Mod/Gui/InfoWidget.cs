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
using System.IO;
using System.Reflection;

namespace FezGame.Mod.Gui {
    public class InfoWidget : GuiWidget {

        protected DateTime BuildDate;

        [ServiceDependency]
        public IGameLevelManager LevelManager { get; set; }
        [ServiceDependency]
        public IPlayerManager PlayerManager { get; set; }

        public SpriteFont Font;

        public InfoWidget(Game game) 
            : base(game) {
            Font = FontManager.Small;

            BuildDate = ReadBuildDate();
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
                GuiHandler.GTR.DrawShadowedText(GuiHandler.SpriteBatch, Font, informations[i], new Vector2(Position.X, Position.Y + i * lineHeight) + Offset, Foreground, viewScale);
            }
        }

        public virtual string[] GetInformations() {
            return new string[] {
                "Build Date " + BuildDate,
                "Level: " + (LevelManager.Name ?? "(none)"),
                "Gomez Position: " + (PlayerManager != null ? (" (" + ToString(PlayerManager.Position) + ")") : "(none)"),
                "Trile Set: " + (LevelManager.TrileSet != null ? LevelManager.TrileSet.Name : "(none)"),
                "Current View: " + CameraManager.Viewpoint
            };
        }

        public static string ToString(Vector2 v) {
            return v.X + ", " + v.Y;
        }

        public static string ToString(Vector3 v) {
            return v.X + ", " + v.Y + ", " + v.Z;
        }

        protected static DateTime ReadBuildDate() {
            string location = Assembly.GetCallingAssembly().Location;
            byte[] array = new byte[2048];
            Stream stream = null;
            try {
                stream = new FileStream(location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream.Read(array, 0, 2048);
            } finally {
                if (stream != null) {
                    stream.Close();
                }
            }
            int num = BitConverter.ToInt32(array, 60);
            int num2 = BitConverter.ToInt32(array, num + 8);
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0);
            dateTime = dateTime.AddSeconds((double) num2);
            dateTime = dateTime.AddHours((double) TimeZone.CurrentTimeZone.GetUtcOffset(dateTime).Hours);
            return dateTime;
        }

    }
}

