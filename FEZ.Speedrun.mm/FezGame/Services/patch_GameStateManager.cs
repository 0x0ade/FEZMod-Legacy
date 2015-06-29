using System;
using FezGame.Speedrun;

namespace FezGame.Services {
    public class patch_GameStateManager {

        public void orig_ToggleInventory() {
        }

        public void ToggleInventory() {
            if (FezSpeedrun.ToolAssistedSpeedrun) {
                return;
            }

            orig_ToggleInventory();
        }

    }
}

