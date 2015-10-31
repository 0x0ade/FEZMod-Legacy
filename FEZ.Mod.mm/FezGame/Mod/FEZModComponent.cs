using Microsoft.Xna.Framework;
using FezEngine.Tools;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezGame.Services;
using FezEngine.Components;

namespace FezGame.Mod {
    public class FEZModComponent : GameComponent {

        [ServiceDependency]
        public ISoundManager SoundManager { get; set; }
        [ServiceDependency]
        public IGameService GameService { get; set; }
        [ServiceDependency]
        public IGameStateManager GameState { get; set; }
        [ServiceDependency]
        public IGameCameraManager CameraManager { get; set; }
        [ServiceDependency]
        public IPlayerManager PlayerManager { get; set; }
        [ServiceDependency]
        public IGameLevelManager LevelManager { get; set; }
        [ServiceDependency]
        public IInputManager InputManager { get; set; }
        [ServiceDependency]
        public IContentManagerProvider CMProvider { private get; set; }

        public FEZModComponent(Game game) 
            : base(game) {
        }

        public override void Initialize() {
        }

        public override void Update(GameTime gameTime) {
            if (FEZMod.OverridePixelsPerTrixel != 0) {
                CameraManager.PixelsPerTrixel = FEZMod.OverridePixelsPerTrixel;
            }

            if (FEZMod.LoadingLevel != null) {
                LevelManager.ChangeLevel(FEZMod.LoadingLevel);
                GameState.ScheduleLoadEnd = true;
                FEZMod.LoadingLevel = null;
            }

        }

    }
}

