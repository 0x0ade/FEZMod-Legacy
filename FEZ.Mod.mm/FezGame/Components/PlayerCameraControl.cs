using System;
using FezGame;
using FezGame.Components;
using FezGame.Services;
using FezGame.Structure;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FezGame.Mod;

namespace FezGame.Components {
    public class PlayerCameraControl {

        public static bool FEZometric = false;

        private Vector2 lastFactors;

        public IGameStateManager orig_get_GameState() {
            return null;
        }
        public IGameStateManager get_GameState() {
            return orig_get_GameState();
        }

        public IInputManager orig_get_InputManager() {
            return null;
        }
        public IInputManager get_InputManager() {
            return orig_get_InputManager();
        }

        public IMouseStateManager orig_get_MouseState() {
            return null;
        }
        public IMouseStateManager get_MouseState() {
            return orig_get_MouseState();
        }

        public IGameCameraManager orig_get_CameraManager() {
            return null;
        }
        public IGameCameraManager get_CameraManager() {
            return orig_get_CameraManager();
        }

        public GraphicsDevice orig_get_GraphicsDevice() {
            return null;
        }
        public GraphicsDevice get_GraphicsDevice() {
            return orig_get_GraphicsDevice();
        }

        public void orig_RotateViewLeft() {
        }
        public void RotateViewLeft() {
            orig_RotateViewLeft();
        }

        public void orig_RotateViewRight() {
        }
        public void RotateViewRight() {
            orig_RotateViewRight();
        }

        public void orig_Update(GameTime gameTime) {
        }
        public void Update(GameTime gameTime) {
            if (!FEZMod.EnableFEZometric) {
                orig_Update(gameTime);
                return;
            }

            if (get_InputManager().OpenInventory == FezButtonState.Pressed) {
                FEZometric = false;
            }

            //TODO Clearify variable names
            if (FEZometric && get_MouseState().LeftButton.State != MouseButtonStates.Dragging && get_InputManager().FreeLook != Vector2.Zero && !get_GameState().InMap && get_CameraManager().Viewpoint != Viewpoint.Perspective) {
                Vector3 vector3 = Vector3.Transform(get_CameraManager().Direction, Matrix.CreateFromAxisAngle(Vector3.Up, 1.570796f));
                Vector3 to1 = Vector3.Transform(get_CameraManager().Direction, Matrix.CreateFromAxisAngle(vector3, -1.570796f));
                Vector2 vector2 = get_InputManager().FreeLook / 6.875f;
                float step = 0.05f;
                Vector3 to2 = FezMath.Slerp(FezMath.Slerp(get_CameraManager().Direction, vector3, vector2.X), to1, vector2.Y);
                if (!get_CameraManager().ActionRunning) {
                    get_CameraManager().AlterTransition(FezMath.Slerp(get_CameraManager().Direction, to2, step));
                } else {
                    get_CameraManager().Direction = FezMath.Slerp(get_CameraManager().Direction, to2, step);
                }
                return;
            }

            orig_Update(gameTime);
        }

    }
}

