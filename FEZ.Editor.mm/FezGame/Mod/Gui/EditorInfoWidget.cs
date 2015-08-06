using System;
using Microsoft.Xna.Framework;
using FezGame.Components;

namespace FezGame.Mod.Gui {
    public class EditorInfoWidget : InfoWidget {
        
        public EditorInfoWidget(Game game) 
            : base(game) {
        }

        public override string[] GetInformations() {
            ILevelEditor LevelEditor = ((ILevelEditor) GuiHandler);

            string[] infoEditor = new string[] {
                "Hovered Trile ID: " + (LevelEditor.HoveredTrile != null ? LevelEditor.HoveredTrile.TrileId.ToString() : "(none)"),
                "Hovered Trile: " + (LevelEditor.HoveredTrile != null ? (LevelEditor.HoveredTrile.Trile.Name + " (" + LevelEditor.HoveredTrile.Emplacement.X + ", " + LevelEditor.HoveredTrile.Emplacement.Y + ", " + LevelEditor.HoveredTrile.Emplacement.Z + ")") : "(none)"),
                "Current Trile ID: " + LevelEditor.TrileId,
                "Current Trile: " + (LevelManager.TrileSet != null && LevelManager.TrileSet.Triles.ContainsKey(LevelEditor.TrileId) ? LevelManager.TrileSet.Triles[LevelEditor.TrileId].Name : "(none)"),
            };

            string[] info = base.GetInformations();
            string[] infoSum = new string[info.Length + infoEditor.Length];
            info.CopyTo(infoSum, 0);
            infoEditor.CopyTo(infoSum, info.Length);
            return infoSum;
        }

    }
}

