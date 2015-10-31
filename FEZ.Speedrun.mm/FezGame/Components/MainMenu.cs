namespace FezGame.Components {
    public class MainMenu {

        public void orig_ContinueGame() {
        }

        //MainMenu overrides the MenuBase ContinueGame without calling it anymore
        public void ContinueGame() {
            orig_ContinueGame();
            MenuBase.triggerClock();
        }

    }
}

