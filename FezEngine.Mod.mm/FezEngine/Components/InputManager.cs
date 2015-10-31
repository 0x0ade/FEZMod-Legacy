#pragma warning disable 626
using System;
using System.Collections.Generic;
using FezGame.Mod;
using FezEngine.Structure.Input;
using Microsoft.Xna.Framework;

namespace FezEngine.Components {
    public class InputManager {
        
        public extern ControllerIndex orig_get_ActiveControllers();
        public extern void orig_set_ActiveControllers(ControllerIndex value);
        public extern FezButtonState orig_get_GrabThrow();
        public extern void orig_set_GrabThrow(FezButtonState value);
        public extern Vector2 orig_get_Movement();
        public extern void orig_set_Movement(Vector2 value);
        public extern Vector2 orig_get_FreeLook();
        public extern void orig_set_FreeLook(Vector2 value);
        public extern FezButtonState orig_get_Jump();
        public extern void orig_set_Jump(FezButtonState value);
        public extern FezButtonState orig_get_Back();
        public extern void orig_set_Back(FezButtonState value);
        public extern FezButtonState orig_get_OpenInventory();
        public extern void orig_set_OpenInventory(FezButtonState value);
        public extern FezButtonState orig_get_Start();
        public extern void orig_set_Start(FezButtonState value);
        public extern FezButtonState orig_get_RotateLeft();
        public extern void orig_set_RotateLeft(FezButtonState value);
        public extern FezButtonState orig_get_RotateRight();
        public extern void orig_set_RotateRight(FezButtonState value);
        public extern FezButtonState orig_get_CancelTalk();
        public extern void orig_set_CancelTalk(FezButtonState value);
        public extern FezButtonState orig_get_Up();
        public extern void orig_set_Up(FezButtonState value);
        public extern FezButtonState orig_get_Down();
        public extern void orig_set_Down(FezButtonState value);
        public extern FezButtonState orig_get_Left();
        public extern void orig_set_Left(FezButtonState value);
        public extern FezButtonState orig_get_Right();
        public extern void orig_set_Right(FezButtonState value);
        public extern FezButtonState orig_get_ClampLook();
        public extern void orig_set_ClampLook(FezButtonState value);
        public extern FezButtonState orig_get_FpsToggle();
        public extern void orig_set_FpsToggle(FezButtonState value);
        public extern FezButtonState orig_get_ExactUp();
        public extern void orig_set_ExactUp(FezButtonState value);
        public extern FezButtonState orig_get_MapZoomIn();
        public extern void orig_set_MapZoomIn(FezButtonState value);
        public extern FezButtonState orig_get_MapZoomOut();
        public extern void orig_set_MapZoomOut(FezButtonState value);
        
        public ControllerIndex ActiveControllers {            
            get {
                if (FakeInputHelper.get_ActiveControllers != null) {
                    return FakeInputHelper.get_ActiveControllers();
                }
                return orig_get_ActiveControllers();
            }
            private set {
                if (FakeInputHelper.set_ActiveControllers != null) {
                    FakeInputHelper.set_ActiveControllers(value);
                    return;
                }
                orig_set_ActiveControllers(value);
            }
        }
        public FezButtonState GrabThrow {            
            get {
                if (FakeInputHelper.get_GrabThrow != null) {
                    return FakeInputHelper.get_GrabThrow();
                }
                return orig_get_GrabThrow();
            }
            private set {
                if (FakeInputHelper.set_GrabThrow != null) {
                    FakeInputHelper.set_GrabThrow(value);
                    return;
                }
                orig_set_GrabThrow(value);
            }
        }
        public Vector2 Movement {            
            get {
                if (FakeInputHelper.get_Movement != null) {
                    return FakeInputHelper.get_Movement();
                }
                return orig_get_Movement();
            }
            private set {
                if (FakeInputHelper.set_Movement != null) {
                    FakeInputHelper.set_Movement(value);
                    return;
                }
                orig_set_Movement(value);
            }
        }
        public Vector2 FreeLook {            
            get {
                if (FakeInputHelper.get_FreeLook != null) {
                    return FakeInputHelper.get_FreeLook();
                }
                return orig_get_FreeLook();
            }
            private set {
                if (FakeInputHelper.set_FreeLook != null) {
                    FakeInputHelper.set_FreeLook(value);
                    return;
                }
                orig_set_FreeLook(value);
            }
        }
        public FezButtonState Jump {            
            get {
                if (FakeInputHelper.get_Jump != null) {
                    return FakeInputHelper.get_Jump();
                }
                return orig_get_Jump();
            }
            private set {
                if (FakeInputHelper.set_Jump != null) {
                    FakeInputHelper.set_Jump(value);
                    return;
                }
                orig_set_Jump(value);
            }
        }
        public FezButtonState Back {            
            get {
                if (FakeInputHelper.get_Back != null) {
                    return FakeInputHelper.get_Back();
                }
                return orig_get_Back();
            }
            private set {
                if (FakeInputHelper.set_Back != null) {
                    FakeInputHelper.set_Back(value);
                    return;
                }
                orig_set_Back(value);
            }
        }
        public FezButtonState OpenInventory {            
            get {
                if (FakeInputHelper.get_OpenInventory != null) {
                    return FakeInputHelper.get_OpenInventory();
                }
                return orig_get_OpenInventory();
            }
            private set {
                if (FakeInputHelper.set_OpenInventory != null) {
                    FakeInputHelper.set_OpenInventory(value);
                    return;
                }
                orig_set_OpenInventory(value);
            }
        }
        public FezButtonState Start {            
            get {
                if (FakeInputHelper.get_Start != null) {
                    return FakeInputHelper.get_Start();
                }
                return orig_get_Start();
            }
            private set {
                if (FakeInputHelper.set_Start != null) {
                    FakeInputHelper.set_Start(value);
                    return;
                }
                orig_set_Start(value);
            }
        }
        public FezButtonState RotateLeft {            
            get {
                if (FakeInputHelper.get_RotateLeft != null) {
                    return FakeInputHelper.get_RotateLeft();
                }
                return orig_get_RotateLeft();
            }
            private set {
                if (FakeInputHelper.set_RotateLeft != null) {
                    FakeInputHelper.set_RotateLeft(value);
                    return;
                }
                orig_set_RotateLeft(value);
            }
        }
        public FezButtonState RotateRight {            
            get {
                if (FakeInputHelper.get_RotateRight != null) {
                    return FakeInputHelper.get_RotateRight();
                }
                return orig_get_RotateRight();
            }
            private set {
                if (FakeInputHelper.set_RotateRight != null) {
                    FakeInputHelper.set_RotateRight(value);
                    return;
                }
                orig_set_RotateRight(value);
            }
        }
        public FezButtonState CancelTalk {            
            get {
                if (FakeInputHelper.get_CancelTalk != null) {
                    return FakeInputHelper.get_CancelTalk();
                }
                return orig_get_CancelTalk();
            }
            private set {
                if (FakeInputHelper.set_CancelTalk != null) {
                    FakeInputHelper.set_CancelTalk(value);
                    return;
                }
                orig_set_CancelTalk(value);
            }
        }
        public FezButtonState Up {            
            get {
                if (FakeInputHelper.get_Up != null) {
                    return FakeInputHelper.get_Up();
                }
                return orig_get_Up();
            }
            private set {
                if (FakeInputHelper.set_Up != null) {
                    FakeInputHelper.set_Up(value);
                    return;
                }
                orig_set_Up(value);
            }
        }
        public FezButtonState Down {            
            get {
                if (FakeInputHelper.get_Down != null) {
                    return FakeInputHelper.get_Down();
                }
                return orig_get_Down();
            }
            private set {
                if (FakeInputHelper.set_Down != null) {
                    FakeInputHelper.set_Down(value);
                    return;
                }
                orig_set_Down(value);
            }
        }
        public FezButtonState Left {            
            get {
                if (FakeInputHelper.get_Left != null) {
                    return FakeInputHelper.get_Left();
                }
                return orig_get_Left();
            }
            private set {
                if (FakeInputHelper.set_Left != null) {
                    FakeInputHelper.set_Left(value);
                    return;
                }
                orig_set_Left(value);
            }
        }
        public FezButtonState Right {            
            get {
                if (FakeInputHelper.get_Right != null) {
                    return FakeInputHelper.get_Right();
                }
                return orig_get_Right();
            }
            private set {
                if (FakeInputHelper.set_Right != null) {
                    FakeInputHelper.set_Right(value);
                    return;
                }
                orig_set_Right(value);
            }
        }
        public FezButtonState ClampLook {            
            get {
                if (FakeInputHelper.get_ClampLook != null) {
                    return FakeInputHelper.get_ClampLook();
                }
                return orig_get_ClampLook();
            }
            private set {
                if (FakeInputHelper.set_ClampLook != null) {
                    FakeInputHelper.set_ClampLook(value);
                    return;
                }
                orig_set_ClampLook(value);
            }
        }
        public FezButtonState FpsToggle {            
            get {
                if (FakeInputHelper.get_FpsToggle != null) {
                    return FakeInputHelper.get_FpsToggle();
                }
                return orig_get_FpsToggle();
            }
            private set {
                if (FakeInputHelper.set_FpsToggle != null) {
                    FakeInputHelper.set_FpsToggle(value);
                    return;
                }
                orig_set_FpsToggle(value);
            }
        }
        public FezButtonState ExactUp {            
            get {
                if (FakeInputHelper.get_ExactUp != null) {
                    return FakeInputHelper.get_ExactUp();
                }
                return orig_get_ExactUp();
            }
            private set {
                if (FakeInputHelper.set_ExactUp != null) {
                    FakeInputHelper.set_ExactUp(value);
                    return;
                }
                orig_set_ExactUp(value);
            }
        }
        public FezButtonState MapZoomIn {            
            get {
                if (FakeInputHelper.get_MapZoomIn != null) {
                    return FakeInputHelper.get_MapZoomIn();
                }
                return orig_get_MapZoomIn();
            }
            private set {
                if (FakeInputHelper.set_MapZoomIn != null) {
                    FakeInputHelper.set_MapZoomIn(value);
                    return;
                }
                orig_set_MapZoomIn(value);
            }
        }
        public FezButtonState MapZoomOut {            
            get {
                if (FakeInputHelper.get_MapZoomOut != null) {
                    return FakeInputHelper.get_MapZoomOut();
                }
                return orig_get_MapZoomOut();
            }
            private set {
                if (FakeInputHelper.set_MapZoomOut != null) {
                    FakeInputHelper.set_MapZoomOut(value);
                    return;
                }
                orig_set_MapZoomOut(value);
            }
        }
        
        public void orig_Update(GameTime gameTime) {
        }
        
        public void Update(GameTime gameTime) {
            FakeInputHelper.Updating = true;
            
            orig_Update(gameTime);
            
            FakeInputHelper.PreUpdate(gameTime);
            
            foreach (KeyValuePair<CodeInputAll, FezButtonState> pair in FakeInputHelper.Overrides) {
                switch (pair.Key) {
                    case CodeInputAll.Back:
                        Back = pair.Value;
                        break;
                    case CodeInputAll.Start:
                        Start = pair.Value;
                        break;
                    case CodeInputAll.Jump:
                        Jump = pair.Value;
                        break;
                    case CodeInputAll.GrabThrow:
                        GrabThrow = pair.Value;
                        break;
                    case CodeInputAll.CancelTalk:
                        CancelTalk = pair.Value;
                        break;
                    case CodeInputAll.Down:
                        Down = pair.Value;
                        if (pair.Value.IsDown()) {
                            Movement = new Vector2(Movement.X, -1f);
                        }
                        break;
                    case CodeInputAll.Up:
                        ExactUp = Up = pair.Value;
                        if (pair.Value.IsDown()) {
                            Movement = new Vector2(Movement.X, 1f);
                        }
                        break;
                    case CodeInputAll.Left:
                        Left = pair.Value;
                        if (pair.Value.IsDown()) {
                            Movement = new Vector2(-1f, Movement.Y);
                        }
                        break;
                    case CodeInputAll.Right:
                        Right = pair.Value;
                        if (pair.Value.IsDown()) {
                            Movement = new Vector2(1f, Movement.Y);
                        }
                        break;
                    case CodeInputAll.OpenInventory:
                        OpenInventory = pair.Value;
                        break;
                    case CodeInputAll.RotateLeft:
                        RotateLeft = pair.Value;
                        break;
                    case CodeInputAll.RotateRight:
                        RotateRight = pair.Value;
                        break;
                    case CodeInputAll.MapZoomIn:
                        MapZoomIn = pair.Value;
                        break;
                    case CodeInputAll.MapZoomOut:
                        MapZoomOut = pair.Value;
                        break;
                    case CodeInputAll.FpsToggle:
                        FpsToggle = pair.Value;
                        break;
                    case CodeInputAll.ClampLook:
                        ClampLook = pair.Value;
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
            
            FakeInputHelper.Updating = false;
        }

    }
}

