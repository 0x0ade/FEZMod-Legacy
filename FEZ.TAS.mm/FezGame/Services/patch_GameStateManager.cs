using FezGame.TAS;

namespace FezGame.Services {
    public class patch_GameStateManager {

        public void orig_ToggleInventory() {
        }

        public void ToggleInventory() {
            if (FezTAS.Settings.ToolAssistedSpeedrun) {
                return;
            }

            orig_ToggleInventory();
        }

    }
}

