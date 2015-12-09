using FezGame.Speedrun;

namespace FezGame.Components {
    public class patch_MenuBase {

        public extern void orig_StartNewGame();
        public void StartNewGame() {
            orig_StartNewGame();
            FezSpeedrun.TriggerClock();
        }

        public extern void orig_ContinueGame();
        public void ContinueGame() {
            orig_ContinueGame();
            FezSpeedrun.TriggerClock();
        }

    }
}

