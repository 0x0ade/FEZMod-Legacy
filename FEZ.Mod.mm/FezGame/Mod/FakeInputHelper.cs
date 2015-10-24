using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using FezEngine;
using FezGame.Components;
using FezEngine.Structure.Input;
using System.Collections.Generic;

namespace FezGame.Mod {
    
    public static class FakeInputHelper {
        
        public readonly static List<CodeInput> PressedOverrides = new List<CodeInput>();
        public readonly static List<CodeInput> ReleasedOverrides = new List<CodeInput>();
        public readonly static Dictionary<CodeInput, FezButtonState> Overrides = new Dictionary<CodeInput, FezButtonState>();
        
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

