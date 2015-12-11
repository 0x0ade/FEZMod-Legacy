using FezGame.Speedrun;

namespace FezGame.Services {
    public class patch_GameStateManager {

        public void orig_ToggleInventory() {
        }

        public void ToggleInventory() {
            if (FezSpeedrun.Settings.ToolAssistedSpeedrun) {
                return;
            }

            orig_ToggleInventory();
        }

    }
}

