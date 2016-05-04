using FezGame.Mod;

namespace FezGame.Services {
    public class patch_GameStateManager {

        public void orig_ToggleInventory() {
        }

        public void ToggleInventory() {
            if (FEZMod.DisableInventory) {
                return;
            }

            orig_ToggleInventory();
        }

    }
}

