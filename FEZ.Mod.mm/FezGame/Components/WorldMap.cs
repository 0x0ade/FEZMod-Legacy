using Common;
using System;
using FezGame.Services;
using FezEngine.Components;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using Microsoft.Xna.Framework;
using FezGame;

namespace FezGame.Components {
    public class WorldMap {

        public MapNode FocusNode;
        public bool QuickWarping = false;

        public IGameLevelManager orig_get_LevelManager() {
            return null;
        }

        public IGameLevelManager get_LevelManager() {
            return orig_get_LevelManager();
        }

        public IInputManager orig_get_InputManager() {
            return null;
        }

        public IInputManager get_InputManager() {
            return orig_get_InputManager();
        }

        public IGameStateManager orig_get_GameState() {
            return null;
        }

        public IGameStateManager get_GameState() {
            return orig_get_GameState();
        }

        public IPlayerManager orig_get_PlayerManager() {
            return null;
        }

        public IPlayerManager get_PlayerManager() {
            return orig_get_PlayerManager();
        }

        public void orig_Update(GameTime gameTime) {
        }

        public void Update(GameTime gameTime) {
            orig_Update(gameTime);

            string levelName = null;
            if (FocusNode != null) {
                levelName = FocusNode.LevelName;
            }
            if (get_InputManager().GrabThrow == FezButtonState.Down) {
                levelName = Fez.ForcedLevelName;
            }

            if (get_InputManager().Jump == FezButtonState.Pressed && levelName != null) {
                ModLogger.Log("JAFM", "Warping to "+levelName);
                QuickWarping = true;
                get_GameState().Loading = true;
                get_GameState().InMap = false;
                get_LevelManager().ChangeLevel(levelName);
            }

            if (get_InputManager().Jump == FezButtonState.Released && QuickWarping) {
                get_GameState().Loading = false;
                QuickWarping = false;
            }

            if (get_InputManager().OpenInventory == FezButtonState.Pressed) {
                ModLogger.Log("JAFM", "Switching to FEZometric mode");
                get_GameState().InMap = false;
            }

            if (get_InputManager().ClampLook == FezButtonState.Pressed) {
                IGameLevelManager levelManager = get_LevelManager();
                if (levelManager is GameLevelManager) {
                    ModLogger.Log("JAFM", "Saving level");
                    ((GameLevelManager) levelManager).Save(levelManager.Name);
                }
            }
        }

    }
}

