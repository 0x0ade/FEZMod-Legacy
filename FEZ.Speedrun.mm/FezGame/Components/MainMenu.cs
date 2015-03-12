using Common;
using System;
using FezGame.Speedrun;

namespace FezGame.Components {
    public class MainMenu {

        public void orig_set_StartedGame(bool startedGame) {
        }

        public void set_StartedGame(bool startedGame) {
            orig_set_StartedGame(startedGame);
            if (FezSpeedrun.SpeedrunMode) {
                SpeedrunInfo.Instance.Running = true;
            }
        }

    }
}

