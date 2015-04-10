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
using System.Collections.Generic;

namespace FezGame.Components {
    public class WorldMap {

        public static WorldMap Instance;

        public static Color HighlightColor = new Color();
        public static string HighlightLevel;

        public MapNode FocusNode;
        public bool QuickWarping = false;

        public IGameLevelManager LevelManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IGameStateManager GameState { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IInputManager InputManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IPlayerManager PlayerManager { [MonoModIgnore] get; [MonoModIgnore] set; }

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

        private static void orig_DoSpecial(MapNode.Connection c, Vector3 offset, Vector3 faceVector, float sizeFactor, List<Matrix> instances) {
        }

        private static void DoSpecial(MapNode.Connection c, Vector3 offset, Vector3 faceVector, float sizeFactor, List<Matrix> instances) {
            orig_DoSpecial(c, offset, faceVector, sizeFactor, instances);
            if (HighlightLevel == null) {
                return;
            }
            if (c.Node.LevelName == HighlightLevel) {
                Vector3 backward = Vector3.Backward;
                float scaleFactor = 10f;
                Vector3 vector = backward * scaleFactor + new Vector3(0.05375f);
                Vector3 vector2 = backward * scaleFactor / 2f + offset + faceVector * sizeFactor;
                c.LinkInstances.Add(instances.Count);
                instances.Add(new Matrix(vector2.X, vector2.Y, vector2.Z, 0, 1, 1, 1, 1, vector.X, vector.Y, vector.Z, 0, 0, 0, 0, 0));
            }
        }

    }
}

