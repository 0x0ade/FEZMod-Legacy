using FezGame.TAS;

namespace FezGame.Components {
    public class MainMenu {

        public extern void orig_ContinueGame();
        //MainMenu overrides the MenuBase ContinueGame without calling it anymore
        public void ContinueGame() {
            orig_ContinueGame();
            FezTAS.Start();
        }

    }
}

