using FezGame.Speedrun;
using FezGame.Speedrun.BOT;

namespace FezGame.Components {
    public class MenuBase {

        public void orig_StartNewGame() {
        }

        public void StartNewGame() {
            orig_StartNewGame();
            triggerClock();
        }

        public void orig_ContinueGame() {
        }

        public void ContinueGame() {
            orig_ContinueGame();
            triggerClock();
        }

        internal static void triggerClock() {
            if (FezSpeedrun.SpeedrunMode) {
                if (FezSpeedrun.Clock.Running) {
                    FezSpeedrun.Clock.Running = false; //Forces the clock to reset.
                }
                FezSpeedrun.Clock.Running = true;
                //Initialize BOT if needed
                if (FezSpeedrun.BOTEnabled) {
                    if (TASComponent.Instance.BOT == null) {
                        TASComponent.Instance.BOT = new BOT(TASComponent.Instance);
                    } else {
                        TASComponent.Instance.BOT.Dispose();
                    }
                    TASComponent.Instance.BOT.Initialize();
                }
            }
        }

    }
}

