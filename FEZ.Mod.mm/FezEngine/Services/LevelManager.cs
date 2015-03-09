using System;
using FezEngine.Services;
using FezEngine.Structure;
using MonoMod;

namespace FezEngine.Services {
    [MonoModIgnore]
    public class LevelManager {
        public Level levelData;

        protected IContentManagerProvider orig_get_CMProvider() {
            return null;
        }

        protected IContentManagerProvider get_CMProvider() {
            return orig_get_CMProvider();
        }

        public void orig_ClearArtSatellites() {
        }

        public void ClearArtSatellites() {
            orig_ClearArtSatellites();
        }

    }
}

