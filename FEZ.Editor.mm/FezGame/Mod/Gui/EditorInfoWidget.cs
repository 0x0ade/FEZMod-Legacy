using Microsoft.Xna.Framework;
using FezGame.Components;

namespace FezGame.Mod.Gui {
    public class EditorInfoWidget : InfoWidget {
        
        protected int infoOffs;
        public EditorInfoWidget(Game game) 
            : base(game) {
                string[] infoOld = info;
                info = new string[(infoOffs = info.Length) + 1];
                infoOld.CopyTo(info, 0);
        }
        
        public override string[] GetInfo() {
            base.GetInfo();
            
            ILevelEditor LevelEditor = ((ILevelEditor) GuiHandler);

            info[infoOffs + 0] = "Hovered Trile: " + (LevelEditor.HoveredTrile != null ? (LevelEditor.HoveredTrile.Trile.Name + " (" + LevelEditor.HoveredTrile.Emplacement.X + ", " + LevelEditor.HoveredTrile.Emplacement.Y + ", " + LevelEditor.HoveredTrile.Emplacement.Z + ")") : "(none)");

            return info;
        }

    }
}

