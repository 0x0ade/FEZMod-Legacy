using FezGame.Services;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FezGame.Mod;
using FezGame.Droid;
using MonoMod;

namespace FezGame.Components {
    public class PlayerCameraControl {

        public static bool FEZometric = false;

        public IGameStateManager GameState { [MonoModIgnore] get { return null; } }
        public IInputManager InputManager { [MonoModIgnore] get { return null; } }
        public IMouseStateManager MouseState { [MonoModIgnore] get { return null; } }
        public IGameCameraManager CameraManager { [MonoModIgnore] get { return null; } }
        public GraphicsDevice GraphicsDevice { [MonoModIgnore] get { return null; } }

        public extern void orig_Update(GameTime gameTime);
        public void Update(GameTime gameTime) {
            if (FezDroid.InAndroid) {
                //FEZDroid player camera control is being handled in FezDroidComponent.
                return;
            }
            
            if (!FEZMod.EnableFEZometric) {
                orig_Update(gameTime);
                return;
            }

            if (InputManager.OpenInventory == FezButtonState.Pressed) {
                FEZometric = false;
            }

            //TODO Rename variables
            if (FEZometric && MouseState.LeftButton.State != MouseButtonStates.Dragging && InputManager.FreeLook != Vector2.Zero && !GameState.InMap && CameraManager.Viewpoint != Viewpoint.Perspective) {
                Vector3 vector3 = Vector3.Transform(CameraManager.Direction, Matrix.CreateFromAxisAngle(Vector3.Up, 1.570796f));
                Vector3 to1 = Vector3.Transform(CameraManager.Direction, Matrix.CreateFromAxisAngle(vector3, -1.570796f));
                Vector2 vector2 = InputManager.FreeLook / 6.875f;
                float step = 0.05f;
                Vector3 to2 = FezMath.Slerp(FezMath.Slerp(CameraManager.Direction, vector3, vector2.X), to1, vector2.Y);
                if (!CameraManager.ActionRunning) {
                    CameraManager.AlterTransition(FezMath.Slerp(CameraManager.Direction, to2, step));
                } else {
                    CameraManager.Direction = FezMath.Slerp(CameraManager.Direction, to2, step);
                }
                return;
            }

            orig_Update(gameTime);
        }

    }
}

