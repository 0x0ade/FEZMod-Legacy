namespace FezGame.Components {
    public class patch_GameLevelHost {

        public void orig_DoFullDraw() {
        }

        public void DoFullDraw() {
            orig_DoFullDraw();
            if (SlaveGomezHost.Instance != null) {
                SlaveGomezHost.Instance.DoDraw();
            }
        }

    }
}

