using FezGame.TAS;

namespace FezGame.Components {
    public class patch_MenuBase {

        public extern void orig_StartNewGame();
        public void StartNewGame() {
            orig_StartNewGame();
            FezTAS.Start();
        }

        public extern void orig_ContinueGame();
        public void ContinueGame() {
            orig_ContinueGame();
            FezTAS.Start();
        }

    }
}

