using Common;
using System;
using FezGame.Speedrun;

namespace FezGame.Components {
    public class MenuBase {

        public void orig_StartNewGame() {
        }

        public void StartNewGame() {
            orig_StartNewGame();
            if (FezSpeedrun.SpeedrunMode) {
                if (FezSpeedrun.Clock.Running) {
                    FezSpeedrun.Clock.Running = false; //Forces the clock to reset.
                }
                FezSpeedrun.Clock.Running = true;
            }
        }

    }
}

