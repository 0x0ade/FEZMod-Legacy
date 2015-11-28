using Microsoft.Xna.Framework;
using FezEngine.Tools;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezGame.Services;
using FezEngine.Components;
using FezEngine.Structure.Input;
using Microsoft.Xna.Framework.Input.Touch;
using FezGame.Structure;
using FezEngine;
using System;
using FezEngine.Structure;
using Microsoft.Xna.Framework.Audio;
using FezGame.Components;
using FezGame.Mod;
using System.Collections.Generic;

namespace FezGame.Droid {
    public class FezDroidComponent : DrawableGameComponent {

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
        [ServiceDependency]
        public ICollisionManager CollisionManager { private get; set; }
        
        private SoundEffect swooshLeft;
        private SoundEffect swooshRight;
        private SoundEffect slowSwooshLeft;
        private SoundEffect slowSwooshRight;
        
        public Dictionary<int, Vector2> TouchOrigins = new Dictionary<int, Vector2>();
        public Dictionary<int, double> TouchTimes = new Dictionary<int, double>();
        
        private int turnTouchId;
        private float turnOffset;
        private Vector2 turnLastFactors;
        
        public FezDroidComponent(Game game) 
            : base(game) {
        }

        public override void Initialize() {
            base.Initialize();
        }
        
        protected override void LoadContent() {
            swooshLeft = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateLeft");
            swooshRight = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateRight");
            slowSwooshLeft = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateLeftHalfSpeed");
            slowSwooshRight = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateRightHalfSpeed");
            
            base.LoadContent();
        }

        public override void Update(GameTime gameTime) {
            //Handle touch input in Android mode
            TouchCollection touches = TouchPanel.GetState();
            
            //TODO implement gestures when they work
            //TODO use screen-space touch coordinates (not 0f - 1f) when they work
            
            bool turnReleased = false;

            for (int i = 0; i < touches.Count; i++) {
                TouchLocation tl = touches[i];
                if (tl.State == TouchLocationState.Pressed && (!TouchOrigins.ContainsKey(tl.Id) || TouchOrigins[tl.Id] != tl.Position)) {
                    TouchOrigins[tl.Id] = tl.Position;
                    TouchTimes[tl.Id] = 0d;
                    if (turnTouchId == -1) {
                        turnTouchId = tl.Id;
                        turnReleased = true;
                    }
                    HandleButtonAt(tl.Position);
                }
                if (tl.State == TouchLocationState.Released) {
                    if (turnTouchId == tl.Id) {
                        turnTouchId = -1;
                    }
                    continue;
                }
                
                if (turnTouchId == tl.Id) {
                    turnOffset = TouchOrigins[tl.Id].X;
                }
                
                TouchTimes[tl.Id] += gameTime.ElapsedGameTime.TotalSeconds;
            }
            
            //Modified decompiled code. Hhnnng.
            //TODO reimplement / rename
            Vector3 vector3 = Vector3.Transform(CameraManager.OriginalDirection, Matrix.CreateFromAxisAngle(Vector3.Up, 1.570796f));
            Vector3 to1 = Vector3.Transform(CameraManager.OriginalDirection, Matrix.CreateFromAxisAngle(vector3, -1.570796f));
            Vector2 vector2 = InputManager.FreeLook / (GameState.MenuCubeIsZoomed ? 1.75f : 6.875f);
            float step = 0.1f;
            if (!turnReleased) {
              vector2 = Vector2.Clamp(new Vector2(turnOffset, 0f) / (300f * SettingsManager.GetViewScale(GraphicsDevice)), -Vector2.One, Vector2.One) / (55.0f / 16.0f);
              step = 0.2f;
              turnLastFactors = vector2;
            } else {
              if ((double) turnLastFactors.X > 0.174999997019768) {
                RotateViewRight();
              } else if ((double) turnLastFactors.X < -0.174999997019768) {
                RotateViewLeft();
              }
            }
            vector2 *= new Vector2(3.425f, 1.725f);
            vector2.Y += 0.25f;
            vector2.X += 0.5f;
            Vector3 to2 = FezMath.Slerp(FezMath.Slerp(CameraManager.OriginalDirection, vector3, vector2.X), to1, vector2.Y);
            if (!CameraManager.ActionRunning) {
              CameraManager.AlterTransition(FezMath.Slerp(CameraManager.Direction, to2, step));
            } else {
              CameraManager.Direction = FezMath.Slerp(CameraManager.Direction, to2, step);
            }
            
        }
        
        private void HandleButtonAt(Vector2 pos) {
            if (Intro.Instance != null && Intro.Instance.Enabled && Intro.Instance.Visible) {
                //TODO find a better way to check if in menu
                bool inMenu = false;
                for (int i = 0; i < ServiceHelper.Game.Components.Count; i++) {
                    if (ServiceHelper.Game.Components[i].GetType().Name.Contains("Menu")) {
                        inMenu = true;
                        break;
                    }
                }
                if (!inMenu) {
                    CodeInputAll.Jump.Press();
                    return;
                }
            }
        }
        
        private void RotateViewLeft() {
            //TODO reimplement / rename
            bool flag = PlayerManager.Action == ActionType.GrabTombstone;
            if (CameraManager.Viewpoint == Viewpoint.Perspective || GameState.InMap) {
                CameraManager.OriginalDirection = Vector3.Transform(CameraManager.OriginalDirection, Quaternion.CreateFromAxisAngle(Vector3.Up, -1.570796f));
                if (!GameState.InMenuCube && !GameState.InMap)
                EmitLeft();
            } else if (CameraManager.ChangeViewpoint(CameraManager.Viewpoint.GetRotatedView(-1), (flag ? 2f : 1f) * Math.Abs(1f / CollisionManager.GravityFactor)) && !flag) {
                EmitLeft();
            }
            if (LevelManager.NodeType != LevelNodeType.Lesser || !(PlayerManager.AirTime != TimeSpan.Zero)) {
                return;
            }
            IPlayerManager playerManager = PlayerManager;
            Vector3 vector3 = playerManager.Velocity * CameraManager.Viewpoint.ScreenSpaceMask();
            playerManager.Velocity = vector3;
        }
            
        private void RotateViewRight() {
            //TODO reimplement / rename
            bool flag = PlayerManager.Action == ActionType.GrabTombstone;
            if (CameraManager.Viewpoint == Viewpoint.Perspective || GameState.InMap) {
                CameraManager.OriginalDirection = Vector3.Transform(CameraManager.OriginalDirection, Quaternion.CreateFromAxisAngle(Vector3.Up, 1.570796f));
                if (!GameState.InMenuCube && !GameState.InMap)
                EmitRight();
            } else if (CameraManager.ChangeViewpoint(CameraManager.Viewpoint.GetRotatedView(1), (flag ? 2f : 1f) * Math.Abs(1f / CollisionManager.GravityFactor)) && !flag) {
                EmitRight();
            }
            if (LevelManager.NodeType != LevelNodeType.Lesser || !(PlayerManager.AirTime != TimeSpan.Zero)) {
                return;
            }
            IPlayerManager playerManager = PlayerManager;
            Vector3 vector3 = playerManager.Velocity * CameraManager.Viewpoint.ScreenSpaceMask();
            playerManager.Velocity = vector3;
        }
        
        private void EmitLeft() {
            if ((double) CollisionManager.GravityFactor == 1.0) {
                swooshLeft.Emit();
            } else {
                slowSwooshLeft.Emit();
            }
        }
    
        private void EmitRight() {
            if ((double) CollisionManager.GravityFactor == 1.0) {
                swooshRight.Emit();
            } else {
                slowSwooshRight.Emit();
            }
        }
    
        private void RotateTo(Viewpoint view) {
            if (Math.Abs(CameraManager.Viewpoint.GetDistance(view)) > 1) {
                EmitRight();
            }
            CameraManager.ChangeViewpoint(view);
        }

    }
}

