using Common;
using System;
using FezEngine.Components;
using FezEngine.Structure.Input;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components {
    public class MenuCube {

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

            if (get_InputManager().GrabThrow == FezButtonState.Pressed) {
                if (get_InputManager().Up == FezButtonState.Down) {
                    get_LevelManager().WaterHeight = get_LevelManager().WaterHeight+1f;
                } else if (get_InputManager().Down == FezButtonState.Down) {
                    get_LevelManager().WaterHeight = get_LevelManager().WaterHeight-1f;
                } else {
                    Vector3 pos = get_PlayerManager().Position;
                    ModLogger.Log("JAFM", "GOMEZ: X: " + pos.X + "; Y: " + pos.Y + "; Z:" + pos.Z);
                }
            }
        }

    }
}

