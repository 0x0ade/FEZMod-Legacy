using System;
using FezEngine.Tools;
using FezEngine.Services;
using FezEngine.Structure;

namespace FezGame.Services {
    /// <summary>
    /// Helping class to access new methods in GameLevelManager.
    /// Workaround until MonoMod patches types into the destination assembly.
    /// </summary>
    public static class GameLevelManagerHelper {

        public static ILevelManager LevelManager {
            get {
                return ServiceHelper.Get<ILevelManager>();
            }
        }

        public static Level Level;

        private static Level ChangeLevel__;

        public static Level ChangeLevel_ {
            get {
                return ChangeLevel__;
            }
            private set {
                ChangeLevel__ = value;
            }
        }

        public static void Save(string levelName) {
            LevelManager.Load("JAFM_WORKAROUND_SAVE:"+levelName);
        }

        public static void ChangeLevel(Level level) {
            ChangeLevel_ = level;
            LevelManager.Load("JAFM_WORKAROUND_CHANGELEVEL");
        }

    }
}

