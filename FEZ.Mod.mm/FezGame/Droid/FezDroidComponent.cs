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
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Droid {
    public class FezDroidComponent : DrawableGameComponent {
        
        public static FezDroidComponent Instance;

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
        
        public SpriteBatch SpriteBatch;
        
        private SoundEffect swooshLeft;
        private SoundEffect swooshRight;
        private SoundEffect slowSwooshLeft;
        private SoundEffect slowSwooshRight;
        
        public Dictionary<int, Vector2> TouchOrigins = new Dictionary<int, Vector2>();
        public Dictionary<int, double> TouchTimes = new Dictionary<int, double>();
        
        public Vector2 Drag = Vector2.Zero;
        public bool Dragging = true;
        private int dragTouchId = -1;
        
        private CodeInputAll[] buttonMapping = {
            CodeInputAll.CancelTalk,
            CodeInputAll.Jump,
            CodeInputAll.GrabThrow,
            CodeInputAll.OpenInventory,
            CodeInputAll.Start,
            CodeInputAll.Back
        };
        private Vector2[] buttonPosition = {
            new Vector2(-1f, -2f),
            new Vector2(-2f, -1f),
            new Vector2(-4f, -2f),
            new Vector2(-2f, 2f),
            new Vector2(1f, -1f),
            new Vector2(-1f, -1f)
        };
        private Vector2[] buttonPre = {
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f)
        };
        private Texture2D[] buttonTexture = new Texture2D[6];
        
        public FezDroidComponent(Game game) 
            : base(game) {
            UpdateOrder = 10;
            DrawOrder = 4001;//One above editor
            Instance = this;
        }

        public override void Initialize() {
            base.Initialize();
            
            Vector2 tmpFreeLook = new Vector2(0f, 0f);
            FakeInputHelper.get_FreeLook = delegate() {
                return FakeInputHelper.Updating ? tmpFreeLook : new Vector2(0f, 0f);
            };
            FakeInputHelper.set_FreeLook = delegate(Vector2 value) {
                tmpFreeLook = value;
            };
        }
        
        protected override void LoadContent() {
            base.LoadContent();
            
            buttonTexture[0] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/BBUTTON");
            buttonTexture[1] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/ABUTTON");
            buttonTexture[2] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/XBUTTON");
            buttonTexture[3] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/YBUTTON");
            buttonTexture[4] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/STARTBUTTON");
            buttonTexture[5] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/BACKBUTTON");
            
            swooshLeft = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateLeft");
            swooshRight = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateRight");
            slowSwooshLeft = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateLeftHalfSpeed");
            slowSwooshRight = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateRightHalfSpeed");
        }

        public override void Update(GameTime gameTime) {
            //Handle touch input in Android mode
            TouchCollection touches = TouchPanel.GetState();
            
            //TODO implement gestures when they work
            //TODO use screen-space touch coordinates (not 0f - 1f) when they work
            
            for (int i = 0; i < touches.Count; i++) {
                TouchLocation tl = touches[i];
                if (tl.State == TouchLocationState.Pressed && (!TouchOrigins.ContainsKey(tl.Id) || TouchOrigins[tl.Id] != tl.Position)) {
                    TouchOrigins[tl.Id] = tl.Position;
                    TouchTimes[tl.Id] = gameTime.TotalGameTime.TotalSeconds;
                    bool button = HandleButtonAt(tl);
                    if (dragTouchId == -1 && !button) {
                        dragTouchId = tl.Id;
                        Dragging = true;
                    }
                }
                if (tl.State == TouchLocationState.Released) {
                    if (dragTouchId == tl.Id) {
                        dragTouchId = -1;
                        //PlayerCameraControl resets Drag
                        Dragging = false;
                    }
                    continue;
                }
                
                if (dragTouchId == tl.Id) {
                    Drag = (tl.Position - TouchOrigins[tl.Id]) * 2f;
                }
            }
            
        }
        
        public override void Draw(GameTime gameTime) {
            if (!FEZMod.Preloaded || GraphicsDevice == null || SpriteBatch == null) {
                if (GraphicsDevice != null) {
                    SpriteBatch = new SpriteBatch(GraphicsDevice);
                }
                return;
            }

            Viewport viewport = GraphicsDevice.Viewport;
            float viewScale = GraphicsDevice.GetViewScale();

            GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
            SpriteBatch.BeginPoint();
            
            float buttonScale = 4f * viewScale;
            float buttonWidth = 16f;
            float buttonHeight = 16f;
            
            for (int i = 0; i < buttonMapping.Length; i++) {
                Texture2D tex = buttonTexture[i];
                Vector2 pos = buttonPosition[i];
                Vector2 pre = buttonPre[i];
                SpriteBatch.Draw(tex, 
                    new Vector2(
                        pre.X * viewport.Width + (pos.X - 1) * buttonWidth * buttonScale,
                        pre.Y * viewport.Height + (pos.Y - 1) * buttonHeight * buttonScale
                    ), null,
                    Color.White,
                    0.0f,
                    Vector2.Zero,
                    buttonScale,
                    SpriteEffects.None,
                    0.0f);
            }
            
            SpriteBatch.End();
        }
        
        
        public bool HandleButtonAt(TouchLocation tl) {
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
                }
                return true;
            }
            
            return false;
        }
        
        public void RotateViewLeft() {
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
            
        public void RotateViewRight() {
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
        
        public void EmitLeft() {
            if ((double) CollisionManager.GravityFactor == 1.0) {
                swooshLeft.Emit();
            } else {
                slowSwooshLeft.Emit();
            }
        }
    
        public void EmitRight() {
            if ((double) CollisionManager.GravityFactor == 1.0) {
                swooshRight.Emit();
            } else {
                slowSwooshRight.Emit();
            }
        }
    
        public void RotateTo(Viewpoint view) {
            if (Math.Abs(CameraManager.Viewpoint.GetDistance(view)) > 1) {
                EmitRight();
            }
            CameraManager.ChangeViewpoint(view);
        }

    }
}

