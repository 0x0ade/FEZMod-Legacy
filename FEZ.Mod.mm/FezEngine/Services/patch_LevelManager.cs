using FezEngine.Structure;
using MonoMod;

namespace FezEngine.Services {
    [MonoModIgnore]
    public class patch_LevelManager {
        public Level levelData;

        protected IContentManagerProvider CMProvider { get { return null; }}

        public void ClearArtSatellites() {
        }

    }
}

