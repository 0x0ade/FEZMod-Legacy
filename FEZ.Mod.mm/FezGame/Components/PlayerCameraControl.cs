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
        [ServiceDependency]
        public IPlayerManager PlayerManager { [MonoModIgnore] get {return null; } }
        
        [MonoModIgnore]
        private extern void FollowGomez();

        public extern void orig_Update(GameTime gameTime);
        public void Update(GameTime gameTime) {
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
            
            if (FezDroidComponent.Instance != null && FezDroidComponent.Instance.Drag != Vector2.Zero && (FezDroidComponent.Instance.DragMode == DragMode.Rotate || FezDroidComponent.Instance.DragModeLast == DragMode.Rotate)) {
                //Modified decompiled code. Hhnnng.
                //TODO reimplement / rename
                Vector3 origin = -CameraManager.Viewpoint.ForwardVector();
                Vector3 vector3 = Vector3.Transform(origin, Matrix.CreateFromAxisAngle(Vector3.Up, 1.570796f));
                Vector3 to1 = Vector3.Transform(origin, Matrix.CreateFromAxisAngle(vector3, -1.570796f));
                Vector2 vector2;
                if (GameState.InMap || GameState.InMenuCube || GameState.InFpsMode) {
                    vector2 = FezDroidComponent.Instance.Drag;
                } else {
                    vector2 = new Vector2(FezDroidComponent.Instance.Drag.X, 0f);
                }
                if (FezDroidComponent.Instance.DragMode == DragMode.Rotate) {
                    float step = 0.2f;
                    Vector3 to2 = FezMath.Slerp(FezMath.Slerp(origin, vector3, vector2.X), to1, vector2.Y);
                    if (!CameraManager.ActionRunning) {
                        CameraManager.AlterTransition(FezMath.Slerp(CameraManager.Direction, to2, step));
                    } else {
                        CameraManager.Direction = FezMath.Slerp(CameraManager.Direction, to2, step);
                    }
                } else {
                    if (FezDroidComponent.Instance.Drag.X > 0.26f) {
                        FezDroidComponent.Instance.RotateViewRight();
                    } else if (FezDroidComponent.Instance.Drag.X < -0.26f) {
                        FezDroidComponent.Instance.RotateViewLeft();
                    } else {
                        FezDroidComponent.Instance.RotateTo(CameraManager.Viewpoint);
                    }
                    FezDroidComponent.Instance.Drag = Vector2.Zero;
                }
                
                return;
            }

            orig_Update(gameTime);
        }

    }
}

