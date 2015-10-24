using System;
using System.Collections.Generic;
using FezGame.Mod;
using FezEngine.Structure.Input;
using Microsoft.Xna.Framework;
using MonoMod;

namespace FezEngine.Components {
    public class InputManager {
        
        public ControllerIndex ActiveControllers { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState GrabThrow { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public Vector2 Movement { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public Vector2 FreeLook { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState Jump { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState Back { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState OpenInventory { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState Start { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState RotateLeft { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState RotateRight { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState CancelTalk { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState Up { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState Down { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState Left { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState Right { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState ClampLook { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState FpsToggle { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState ExactUp { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState MapZoomIn { [MonoModIgnore] get; [MonoModIgnore] private set; }
        public FezButtonState MapZoomOut { [MonoModIgnore] get; [MonoModIgnore] private set; }
        
        public void orig_Update(GameTime gameTime) {
        }
        
        public void Update(GameTime gameTime) {
            orig_Update(gameTime);
            
            FakeInputHelper.PreUpdate(gameTime);
            
            foreach (KeyValuePair<CodeInput, FezButtonState> pair in FakeInputHelper.Overrides) {
                switch (pair.Key) {
                    case CodeInput.None:
                        throw new InvalidOperationException("Can't set button state of no button!");
                        //TODO make "none" refer to something like grabthrow or canceltalk
                        break;
                    case CodeInput.Up:
                        ExactUp = Up = pair.Value;
                        Movement = new Vector2(Movement.X, 1f);
                        break;
                    case CodeInput.Down:
                        Down = pair.Value;
                        Movement = new Vector2(Movement.X, -1f);
                        break;
                    case CodeInput.Left:
                        Left = pair.Value;
                        Movement = new Vector2(-1f, Movement.Y);
                        break;
                    case CodeInput.Right:
                        Right = pair.Value;
                        Movement = new Vector2(1f, Movement.Y);
                        break;
                    case CodeInput.SpinLeft:
                        RotateLeft = pair.Value;
                        break;
                    case CodeInput.SpinRight:
                        RotateRight = pair.Value;
                        break;
                    case CodeInput.Jump:
                        Jump = pair.Value;
                        break;
                    default:
                        //TODO get int value and do something special
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            FakeInputHelper.PostUpdate(gameTime);
            
            /*
            Movement = new Vector2(FezButtonStateExtensions.IsDown(KeyboardState.Right) ? 1f : (FezButtonStateExtensions.IsDown(KeyboardState.Left) ? -1f : 0.0f), FezButtonStateExtensions.IsDown(KeyboardState.Up) ? 1f : (FezButtonStateExtensions.IsDown(KeyboardState.Down) ? -1f : 0.0f));
            FreeLook = new Vector2(FezButtonStateExtensions.IsDown(KeyboardState.LookRight) ? 1f : (FezButtonStateExtensions.IsDown(KeyboardState.LookLeft) ? -1f : 0.0f), FezButtonStateExtensions.IsDown(KeyboardState.LookUp) ? 1f : (FezButtonStateExtensions.IsDown(KeyboardState.LookDown) ? -1f : 0.0f));
            Back = KeyboardState.OpenMap;
            Start = KeyboardState.Pause;
            Jump = KeyboardState.Jump;
            GrabThrow = KeyboardState.GrabThrow;
            CancelTalk = KeyboardState.CancelTalk;
            Down = KeyboardState.Down;
            ExactUp = Up = KeyboardState.Up;
            Left = KeyboardState.Left;
            Right = KeyboardState.Right;
            OpenInventory = KeyboardState.OpenInventory;
            RotateLeft = KeyboardState.RotateLeft;
            RotateRight = KeyboardState.RotateRight;
            MapZoomIn = KeyboardState.MapZoomIn;
            MapZoomOut = KeyboardState.MapZoomOut;
            FpsToggle = KeyboardState.FpViewToggle;
            ClampLook = KeyboardState.ClampLook;
            */
        }

    }
}

