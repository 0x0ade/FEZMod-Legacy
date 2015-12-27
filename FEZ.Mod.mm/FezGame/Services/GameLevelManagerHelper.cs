using FezEngine.Tools;
using FezEngine.Services;
using FezEngine.Structure;

namespace FezGame.Services {
    /// <summary>
    /// Helping class to access new methods in GameLevelManager.
    /// Workaround until MonoMod patches types into the destination assembly.
    /// </summary>
    public static class GameLevelManagerHelper {
        
        private static ILevelManager levelManager;
        public static ILevelManager LevelManager {
            get {
                return levelManager ?? (levelManager = ServiceHelper.Get<ILevelManager>());
            }
        }

        public static Level Level;

        public static void Save(string levelName, bool binary = false) {
            ((GameLevelManager) LevelManager).Save(levelName, binary);
        }

        public static void ChangeLevel(Level level) {
            ((GameLevelManager) LevelManager).ChangeLevel(level);
        }

    }
}

