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
                if (SpeedrunInfo.Instance.Running) {
                    SpeedrunInfo.Instance.Running = false;//Forces the LiveSplit timer to re-run.
                }
                SpeedrunInfo.Instance.Running = true;
            }
        }

    }
}

