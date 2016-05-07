namespace FezGame.Services {
    public class patch_GameCameraManager {

        public extern float orig_get_InterpolationSpeed();
        public float get_InterpolationSpeed() {
            if (Fez.LongScreenshot) {
                return 0.06f;
            } else {
                return orig_get_InterpolationSpeed();
            }
        }

        public extern float orig_get_Radius();
        public float get_Radius() {
            if (Fez.LongScreenshot) {
                return 90f;
            } else {
                return orig_get_Radius();
            }
        }

    }
}

