using Common;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using MonoMod;
using FezEngine.Mod;
using FezGame.Mod;

namespace FezGame.Components {
    [MonoModPublic]
	public class patch_MenuBase : MenuBase {

        /*p1*/
        public patch_MenuBase(Game game) : base(game) {
            //no-op
		}
        
        public extern void orig_Initialize();
		public override void Initialize() {
			orig_Initialize();
            
            // FEZMod.InitializeMenu(this);
        
            foreach (MenuLevel current in MenuLevels) {
				if (current != MenuRoot && current.Parent == null) {
					current.Parent = MenuRoot;
				}
			}
		}
        
	}
}
