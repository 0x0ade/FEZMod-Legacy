using System;
using FezEngine.Services;
using FezEngine.Structure;

namespace FezEngine.Services {
    public class LevelManager {
        //This class is going to be ignored by MonoMod.

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

