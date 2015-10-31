namespace FezGame.Services {
    public class GameCameraManager {

        public float orig_get_InterpolationSpeed() {
            return 1f;
        }

        public float get_InterpolationSpeed() {
            if (Fez.LongScreenshot) {
                return 0.06f;
            } else {
                return orig_get_InterpolationSpeed();
            }
        }

        public float orig_get_Radius() {
            return 1f;
        }

        public float get_Radius() {
            if (Fez.LongScreenshot) {
                return 90f;
            } else {
                return orig_get_Radius();
            }
        }

    }
}

