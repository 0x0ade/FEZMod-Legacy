using Common;
using System;
using FezGame.Services;
using FezEngine.Components;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using Microsoft.Xna.Framework;
using FezGame;
using FezGame.Mod;
using MonoMod;

namespace FezGame.Components {
    public class WorldMap {

        public MapNode FocusNode;
        public bool QuickWarping = false;

        public IGameLevelManager LevelManager { [MonoModIgnore] get { return null; } }
        public IGameStateManager GameState { [MonoModIgnore] get { return null; } }
        public IInputManager InputManager { [MonoModIgnore] get { return null; } }
        public IPlayerManager PlayerManager { [MonoModIgnore] get { return null; } }

        public void orig_Update(GameTime gameTime) {
        }

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
                ModLogger.Log("JAFM", "Warping to " + levelName);
                QuickWarping = true;
                GameState.Loading = true;
                GameState.InMap = false;
                LevelManager.ChangeLevel(levelName);
            }

            if (FEZMod.EnableQuickWarp && InputManager.Jump == FezButtonState.Released && QuickWarping) {
                GameState.Loading = false;
                QuickWarping = false;
            }

            if (FEZMod.EnableFEZometric && InputManager.OpenInventory == FezButtonState.Pressed) {
                ModLogger.Log("JAFM", "Switching to FEZometric mode");
                GameState.InMap = false;
                PlayerCameraControl.FEZometric = true;
            }
        }

    }
}

