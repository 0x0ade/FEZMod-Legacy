using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using FezEngine;
using FezGame.Components;
using FezEngine.Structure.Input;
using System.Collections.Generic;

namespace FezGame.Mod {
    
    public static class FakeInputHelper {
        
        public static Func<ControllerIndex> get_ActiveControllers;
        public static Action<ControllerIndex> set_ActiveControllers;
        public static Func<FezButtonState> get_GrabThrow;
        public static Action<FezButtonState> set_GrabThrow;
        public static Func<Vector2> get_Movement;
        public static Action<Vector2> set_Movement;
        public static Func<Vector2> get_FreeLook;
        public static Action<Vector2> set_FreeLook;
        public static Func<FezButtonState> get_Jump;
        public static Action<FezButtonState> set_Jump;
        public static Func<FezButtonState> get_Back;
        public static Action<FezButtonState> set_Back;
        public static Func<FezButtonState> get_OpenInventory;
        public static Action<FezButtonState> set_OpenInventory;
        public static Func<FezButtonState> get_Start;
        public static Action<FezButtonState> set_Start;
        public static Func<FezButtonState> get_RotateLeft;
        public static Action<FezButtonState> set_RotateLeft;
        public static Func<FezButtonState> get_RotateRight;
        public static Action<FezButtonState> set_RotateRight;
        public static Func<FezButtonState> get_CancelTalk;
        public static Action<FezButtonState> set_CancelTalk;
        public static Func<FezButtonState> get_Up;
        public static Action<FezButtonState> set_Up;
        public static Func<FezButtonState> get_Down;
        public static Action<FezButtonState> set_Down;
        public static Func<FezButtonState> get_Left;
        public static Action<FezButtonState> set_Left;
        public static Func<FezButtonState> get_Right;
        public static Action<FezButtonState> set_Right;
        public static Func<FezButtonState> get_ClampLook;
        public static Action<FezButtonState> set_ClampLook;
        public static Func<FezButtonState> get_FpsToggle;
        public static Action<FezButtonState> set_FpsToggle;
        public static Func<FezButtonState> get_ExactUp;
        public static Action<FezButtonState> set_ExactUp;
        public static Func<FezButtonState> get_MapZoomIn;
        public static Action<FezButtonState> set_MapZoomIn;
        public static Func<FezButtonState> get_MapZoomOut;
        public static Action<FezButtonState> set_MapZoomOut;
        
        public readonly static List<CodeInput> PressedOverrides = new List<CodeInput>();
        public readonly static List<CodeInput> ReleasedOverrides = new List<CodeInput>();
        public readonly static Dictionary<CodeInput, FezButtonState> Overrides = new Dictionary<CodeInput, FezButtonState>();
        
        public static bool Updating;
        
        public static void PreUpdate(GameTime gameTime) {
            foreach (CodeInput key in ReleasedOverrides) {
                Overrides.Remove(key);
                PressedOverrides.Remove(key);
            }
            ReleasedOverrides.Clear();
            
            foreach (CodeInput key in PressedOverrides) {
                FezButtonState value;
                if (!Overrides.TryGetValue(key, out value) || value == FezButtonState.Released) {
                    Overrides[key] = FezButtonState.Released;
                    ReleasedOverrides.Add(key);
                    continue;
                }
                Overrides[key] = FezButtonState.Down;
            }
        }
        
        public static void PostUpdate(GameTime gameTime) {
            foreach (KeyValuePair<CodeInput, FezButtonState> pair in Overrides) {
                if (pair.Value == FezButtonState.Pressed && !PressedOverrides.Contains(pair.Key)) {
                    PressedOverrides.Add(pair.Key);
                }
            }
            
            Overrides.Clear();
        }
        
    }
}

