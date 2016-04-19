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
using FezEngine.Mod;
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
        protected static Texture2D pixelTexture;
        
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
            new Vector2(1.5f, 0f),
            new Vector2(-0.5f, 0f)
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
            true,
            true,
            true,
            true,
            true,
            false
        };
        protected Texture2D[] buttonTexture = new Texture2D[6];
        protected bool buttonsEnabled = false;
        protected float buttonWidth = 16f;
        protected float buttonHeight = 16f;
        protected float buttonScaleMain = 6f;
        
        protected Vector2 touchFieldPosition = new Vector2(0f, 0.5f);
        protected Vector2 touchFieldSize = new Vector2(0.4f, 0.5f);
        protected Color touchFieldBackground = new Color(0.75f, 0.75f, 0.75f, 0.25f);
        
        public FezDroidComponent(Game game) 
            : base(game) {
            UpdateOrder = 10;
            DrawOrder = 4001;//One above editor
            Instance = this;
        }

        public override void Initialize() {
            base.Initialize();
            
            Vector2 tmpFreeLook = new Vector2(0f, 0f);
            Func<Vector2> prev_get_FreeLook = FakeInputHelper.get_FreeLook;
            Action<Vector2> prev_set_FreeLook = FakeInputHelper.set_FreeLook;
            FakeInputHelper.get_FreeLook = delegate() {
                return FakeInputHelper.Updating ? (prev_get_FreeLook != null ? prev_get_FreeLook() : tmpFreeLook) : Vector2.Zero;
            };
            FakeInputHelper.set_FreeLook = delegate(Vector2 value) {
                tmpFreeLook = value;
                if (prev_set_FreeLook != null) {
                    prev_set_FreeLook(value);
                }
            };
            
            Vector2 tmpMovement = new Vector2(0f, 0f);
            Func<Vector2> prev_get_Movement = FakeInputHelper.get_Movement;
            Action<Vector2> prev_set_Movement = FakeInputHelper.set_Movement;
            FakeInputHelper.get_Movement = delegate() {
                return FakeInputHelper.Updating || DragMode != DragMode.Move ? (prev_get_Movement != null ? prev_get_Movement() : tmpMovement) : Drag * 4f;
            };
            FakeInputHelper.set_Movement = delegate(Vector2 value) {
                tmpMovement = value;
                if (prev_set_Movement != null) {
                    prev_set_Movement(value);
                }
            };
        }
        
        protected override void LoadContent() {
            FEZModEngine.InvokeGL(delegate() {
                SpriteBatch = new SpriteBatch(GraphicsDevice);
                buttonTexture[0] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/BBUTTON");
                buttonTexture[1] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/ABUTTON");
                buttonTexture[2] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/XBUTTON");
                buttonTexture[3] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/YBUTTON");
                buttonTexture[4] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/STARTBUTTON");
                buttonTexture[5] = CMProvider.Global.Load<Texture2D>("Other Textures/Glyphs/BACKBUTTON");
            });
        }
        
        public override void Update(GameTime gameTime) {
            //Handle touch input in Android mode
            TouchCollection touches = TouchPanel.GetState();
            
            float x1 = touchFieldPosition.X * FezDroid.TouchWidth;
            float y1 = touchFieldPosition.Y * FezDroid.TouchHeight;
            float x2 = x1 + touchFieldSize.X * FezDroid.TouchWidth;
            float y2 = y1 + touchFieldSize.Y * FezDroid.TouchHeight;
            
            for (int i = 0; i < touches.Count; i++) {
                TouchLocation tl = touches[i];
                if (tl.State == TouchLocationState.Pressed && (!TouchOrigins.ContainsKey(tl.Id) || TouchOrigins[tl.Id] != tl.Position)) {
                    TouchOrigins[tl.Id] = tl.Position;
                    TouchTimes[tl.Id] = gameTime.TotalGameTime.TotalSeconds;
                    bool button = HandleButtonAt(tl);
                    if (dragTouchId == -1 && !button) {
                        dragTouchId = tl.Id;
                        DragModeLast = DragMode;
                        DragMode =
                            x1 <= tl.Position.X && tl.Position.X <= x2 &&
                            y1 <= tl.Position.Y && tl.Position.Y <= y2 ?
                            DragMode.Move : DragMode.Rotate;
                    }
                    continue;
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
                    Drag = (tl.Position - TouchOrigins[tl.Id]);
                    Drag.Y = -Drag.Y; //Y == 0 is top, not bottom
                    Drag.X *= 2f / FezDroid.TouchWidth;
                    Drag.Y *= 2f / FezDroid.TouchHeight;
                } else {
                    HandleButtonAt(tl);
                }
            }
            
            buttonsEnabled = !GameState.TimePaused || GameState.InMenuCube || GameState.InMap;
            buttonEnabled[5] = GameState.SaveData != null && LevelManager.Name != null && GameState.SaveData.CanOpenMap && LevelManager.Name != "PYRAMID" && !LevelManager.Name.StartsWith("GOMEZ_HOUSE_END");
            
            for (int i = 0; i < buttonMapping.Length; i++) {
                buttonAlpha[i] = buttonAlpha[i] * 0.95f + (buttonsEnabled && buttonEnabled[i] ? 1f : 0f) * 0.05f;
            }
            
            touchFieldBackground = new Color(0, 0, 0, 0.25f);
            if (DragMode == DragMode.Move) {
                if (Math.Abs(Drag.Y) < 0.3f * FezDroid.TouchHeight) {
                    if (Drag.X >= -0.2f * FezDroid.TouchWidth) {
                        CodeInputAll.Left.Press();
                        touchFieldBackground.R = 255;
                        touchFieldBackground.G = 0;
                    } else if (Drag.X <= 0.2f * FezDroid.TouchWidth) {
                        CodeInputAll.Right.Press();
                        touchFieldBackground.R = 0;
                        touchFieldBackground.G = 255;
                    }
                }
                if (Math.Abs(Drag.X) < 0.3f * FezDroid.TouchWidth) {
                    if (Drag.Y >= -0.2f * FezDroid.TouchHeight) {
                        CodeInputAll.Up.Press();
                        touchFieldBackground.B = 255;
                    } else if (Drag.Y <= 0.2f * FezDroid.TouchHeight) {
                        CodeInputAll.Down.Press();
                    }
                }
            }
        }
        
        public override void Draw(GameTime gameTime) {
            if (!FEZMod.Preloaded || GraphicsDevice == null || SpriteBatch == null) {
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
            
            if (pixelTexture == null) {
                pixelTexture = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                pixelTexture.SetData<Color>(new Color[] { Color.White });
            }

            if (buttonAlpha[0] != 0f) {
                //We simply take the CancelTalk ([0]) state and apply it to the touch area.
                SpriteBatch.Draw(pixelTexture,
                    new Vector2(
                        touchFieldPosition.X * viewport.Width,
                        touchFieldPosition.Y * viewport.Height
                    ), null,
                    touchFieldBackground * buttonAlpha[0],
                    0.0f,
                    Vector2.Zero,
                    new Vector2(
                        touchFieldSize.X * viewport.Width,
                        touchFieldSize.Y * viewport.Height
                    ),
                    SpriteEffects.None,
                    0.0f);
            }
            
            for (int i = 0; i < buttonMapping.Length; i++) {
                if (buttonAlpha[i] == 0f) {
                    continue;
                }
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
                    x1 <= tl.Position.X && tl.Position.X <= x2 &&
                    y1 <= tl.Position.Y && tl.Position.Y <= y2
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
