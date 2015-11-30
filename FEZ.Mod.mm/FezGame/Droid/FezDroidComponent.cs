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
    
    public enum DragMode {
        None,
        Rotate,
        Move
    }
    
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
        public IContentManagerProvider CMProvider { get; set; }
        [ServiceDependency]
        public ICollisionManager CollisionManager { get; set; }
        
        public SpriteBatch SpriteBatch;
        
        public Dictionary<int, Vector2> TouchOrigins = new Dictionary<int, Vector2>();
        public Dictionary<int, double> TouchTimes = new Dictionary<int, double>();
        
        public Vector2 Drag = Vector2.Zero;
        public DragMode DragMode = DragMode.None;
        public DragMode DragModeLast = DragMode.None;
        protected int dragTouchId = -1;
        
        protected CodeInputAll[] buttonMapping = {
            CodeInputAll.CancelTalk,
            CodeInputAll.Jump,
            CodeInputAll.GrabThrow,
            CodeInputAll.OpenInventory,
            CodeInputAll.Start,
            CodeInputAll.Back
        };
        protected Vector2[] buttonPosition = {
            new Vector2(0f, -1f),
            new Vector2(-1f, 0f),
            new Vector2(-3f, -1f),
            new Vector2(-1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(-1f, 0f)
        };
        protected Vector2[] buttonPre = {
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f)
        };
        protected float[] buttonAlpha = {
            0f,
            0f,
            0f,
            0f,
            0f,
            0f
        };
        protected bool[] buttonEnabled = {
            false,
            true,
            false,
            true,
            true,
            false
        };
        protected Texture2D[] buttonTexture = new Texture2D[6];
        protected bool buttonsEnabled = false;
        protected float buttonWidth = 16f;
        protected float buttonHeight = 16f;
        protected float buttonScaleMain = 6f;
        
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
            
            Vector2 tmpMovement = new Vector2(0f, 0f);
            FakeInputHelper.get_Movement = delegate() {
                return FakeInputHelper.Updating || DragMode != DragMode.Move ? tmpMovement : Drag * 2f;
            };
            FakeInputHelper.set_Movement = delegate(Vector2 value) {
                tmpMovement = value;
            };
        }
        
        protected override void LoadContent() {
            base.LoadContent();
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
                        DragModeLast = DragMode;
                        DragMode = tl.Position.X <= 0.4f && tl.Position.Y >= 0.5f ? DragMode.Move : DragMode.Rotate;
                    } else if (dragTouchId != -1) {
                        //FIXME FNA multitouch support is... T.T
                        dragTouchId = -1;
                    }
                    return;
                }
                if (tl.State == TouchLocationState.Released) {
                    if (dragTouchId == tl.Id) {
                        dragTouchId = -1;
                        //PlayerCameraControl resets Drag
                        DragModeLast = DragMode;
                        DragMode = DragMode.None;
                    }
                    continue;
                }
                
                if (dragTouchId == tl.Id) {
                    Drag = (tl.Position - TouchOrigins[tl.Id]) * 2f;
                } else {
                    HandleButtonAt(tl);
                }
            }
            
            buttonsEnabled = !GameState.TimePaused || GameState.InMenuCube || GameState.InMap;
            
            for (int i = 0; i < buttonMapping.Length; i++) {
                buttonAlpha[i] = buttonAlpha[i] * 0.95f + (buttonsEnabled && buttonEnabled[i] ? 1f : 0f) * 0.05f;
            }
            
        }
        
        public override void Draw(GameTime gameTime) {
            if (!FEZMod.Preloaded || GraphicsDevice == null || SpriteBatch == null) {
                if (GraphicsDevice != null) {
                    SpriteBatch = new SpriteBatch(GraphicsDevice);
                    buttonTexture[0] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/BBUTTON");
                    buttonTexture[1] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/ABUTTON");
                    buttonTexture[2] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/XBUTTON");
                    buttonTexture[3] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/YBUTTON");
                    buttonTexture[4] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/STARTBUTTON");
                    buttonTexture[5] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/BACKBUTTON");
                }
                return;
            }
            
            if (Intro.Instance != null && Intro.Instance.Enabled && Intro.Instance.Visible) {
                return;
            }
            
            Viewport viewport = GraphicsDevice.Viewport;
            float viewScale = GraphicsDevice.GetViewScale();

            GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
            SpriteBatch.BeginPoint();
            
            float buttonScale = buttonScaleMain * viewScale;
            
            for (int i = 0; i < buttonMapping.Length; i++) {
                Texture2D tex = buttonTexture[i];
                Vector2 pos = buttonPosition[i];
                Vector2 pre = buttonPre[i];
                SpriteBatch.Draw(tex, 
                    new Vector2(
                        pre.X * viewport.Width + (pos.X - 1) * buttonWidth * buttonScale,
                        pre.Y * viewport.Height + (pos.Y - 1) * buttonHeight * buttonScale
                    ), null,
                    new Color(1f, 1f, 1f, buttonAlpha[i]),
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
                if (!inMenu && tl.State == TouchLocationState.Pressed) {
                    CodeInputAll.Jump.Press();
                }
                return true;
            }
            
            if (!buttonsEnabled) {
                return true;
            }
            
            Viewport viewport = GraphicsDevice.Viewport;
            float viewScale = GraphicsDevice.GetViewScale();
            float buttonScale = buttonScaleMain * viewScale;
            
            for (int i = 0; i < buttonMapping.Length; i++) {
                if (!buttonEnabled[i]) {
                    continue;
                }
                Vector2 pos = buttonPosition[i];
                Vector2 pre = buttonPre[i];
                float x1 = pre.X * viewport.Width + (pos.X - 1) * buttonWidth * buttonScale;
                float y1 = pre.Y * viewport.Height + (pos.Y - 1) * buttonHeight * buttonScale;
                float x2 = x1 + buttonWidth * buttonScale;
                float y2 = y1 + buttonHeight * buttonScale;
                if (
                    x1 <= tl.Position.X * viewport.Width && tl.Position.X * viewport.Width <= x2 &&
                    y1 <= tl.Position.Y * viewport.Height && tl.Position.Y * viewport.Height <= y2
                ) {
                    buttonMapping[i].Hold();
                    return true;
                }
            }
            
            return false;
        }
        
        public void RotateViewLeft() {
            CodeInputAll.RotateLeft.Press();
        }
            
        public void RotateViewRight() {
            CodeInputAll.RotateRight.Press();
        }
        
        public void RotateTo(Viewpoint view) {
            CameraManager.ChangeViewpoint(view, 0.2f);
        }

    }
}

