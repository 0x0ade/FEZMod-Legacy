#pragma warning disable 436
using FezGame.Mod;
using FezEngine.Mod;
using FezGame.Services;
using FezEngine.Components;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using Microsoft.Xna.Framework;
using MonoMod;
using System.Collections.Generic;

namespace FezGame.Components {
    public class WorldMap {

        public static WorldMap Instance;

        public MapNode FocusNode;

        public IGameLevelManager LevelManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IGameStateManager GameState { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IInputManager InputManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IPlayerManager PlayerManager { [MonoModIgnore] get; [MonoModIgnore] set; }

        [MonoModIgnore]
        private extern void Exit();

        public extern void orig_Update(GameTime gameTime);
        public void Update(GameTime gameTime) {
            orig_Update(gameTime);

            string levelName = null;
            if (FocusNode != null) {
                levelName = FocusNode.LevelName;
            }
            if (InputManager.GrabThrow == FezButtonState.Down) {
                levelName = Fez.ForcedLevelName;
            }

            if (FEZMod.EnableQuickWarp && InputManager.Jump == FezButtonState.Pressed && levelName != null) {
                ModLogger.Log("FEZMod", "Warping to " + levelName);
                GameState.InMap = false;
                FEZMod.LoadingLevel = levelName;
            }

            if (FEZMod.EnableFEZometric && InputManager.OpenInventory == FezButtonState.Pressed) {
                ModLogger.Log("FEZMod", "Switching to FEZometric mode");
                GameState.InMap = false;
                PlayerCameraControl.FEZometric = true;
            }
        }

    }
}

