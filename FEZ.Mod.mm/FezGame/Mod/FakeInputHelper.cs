using System;
using Microsoft.Xna.Framework;
using FezEngine.Structure.Input;
using System.Collections.Generic;

namespace FezGame.Mod {
    
    public struct Sequential_Key_Duration {
        public CodeInputAll Key;
        public double Duration;
    }
    
    public class KeySequence {
        public List<List<CodeInputAll>> Keys;
        public int Current;
        private int frame;
        
        public KeySequence FillKeys(int frames) {
            if (Keys == null) {
                Keys = new List<List<CodeInputAll>>(Math.Max(16, frames));
            }
            while ((Keys.Count - 1) < frames) {
                Keys.Add(new List<CodeInputAll>(4));
            }
            return this;
        }
        
        public KeySequence AddFrame(CodeInputAll key) {
            return AddFrame().Add(key, frame);
        }
        
        public KeySequence Add(CodeInputAll key) {
            return Add(key, frame);
        }
        
        public KeySequence AddFrame() {
            frame++;
            return FillKeys(frame);
        }
        
        public KeySequence Add(CodeInputAll key, int frame) {
            FillKeys(frame);
            Keys[frame].Add(key);
            this.frame = frame;
            return this;
        }
    }
    
    public static class FakeInputHelper {
        
        //key hooks
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
        
        //fake input overrides
        public readonly static List<CodeInputAll> PressedOverrides = new List<CodeInputAll>();
        public readonly static List<CodeInputAll> ReleasedOverrides = new List<CodeInputAll>();
        public readonly static Dictionary<CodeInputAll, FezButtonState> Overrides = new Dictionary<CodeInputAll, FezButtonState>();
        
        //other fields / properties
        public static bool Updating;
        
        //fake input timing helpers
        private static Dictionary<CodeInputAll, double> keyTimes = new Dictionary<CodeInputAll, double>();
        private static List<CodeInputAll> keyTimesApplied = new List<CodeInputAll>();
        
        //fake input sequential helpers
        private static List<Sequential_Key_Duration> keysHold = new List<Sequential_Key_Duration>();
        private static List<Sequential_Key_Duration> tmpKeysHold = new List<Sequential_Key_Duration>();
        private static Sequential_Key_Duration tmpKeyHold = new Sequential_Key_Duration();
        
        public static List<KeySequence> Sequences = new List<KeySequence>();
        
        //hooks
        public static void PreUpdate(GameTime gameTime) {
            //sequential input
            for (int i = 0; i < Sequences.Count; i++) {
                KeySequence seq = Sequences[i];
                List<CodeInputAll> current = seq.Keys[seq.Current];
                
                for (int ki = 0; ki < current.Count; ki++) {
                    current[ki].Hold(-1d);
                }
                
                seq.Current++;
                if (seq.Keys.Count <= seq.Current) {
                    Sequences.RemoveAt(i);
                    seq.Current = 0;
                    i--;
                }
            }
            
            //extended hold
            tmpKeysHold.Clear();
            tmpKeysHold.AddRange(keysHold);
            keysHold.Clear();
            for (int i = 0; i < tmpKeysHold.Count; i++) {
                Sequential_Key_Duration keyHold = tmpKeysHold[i];
                keyHold.Key.Hold(keyHold.Duration);
            }
            
            keyTimesApplied.Clear();
            
            foreach (CodeInputAll key in PressedOverrides) {
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
            foreach (CodeInputAll key in ReleasedOverrides) {
                PressedOverrides.Remove(key);
            }
            ReleasedOverrides.Clear();

            
            foreach (KeyValuePair<CodeInputAll, FezButtonState> pair in Overrides) {
                if (pair.Value == FezButtonState.Pressed && !PressedOverrides.Contains(pair.Key)) {
                    PressedOverrides.Add(pair.Key);
                }
            }
            
            Overrides.Clear();
        }
        
        //fake input helpers
        public static int Timed(this CodeInputAll key, double max, bool apply = true) {
            if (max <= 0d) {
                return 0;
            }
            
            double time;
            apply = apply && !keyTimesApplied.Contains(key);
            
            if (!keyTimes.TryGetValue(key, out time)) {
                if (apply) {
                    keyTimes[key] = 0d;
                    keyTimesApplied.Add(key);
                }
                return 1;
            }
            
            if (apply) {
                time = keyTimes[key] = time + FEZMod.UpdateGameTime.ElapsedGameTime.TotalSeconds;
                keyTimesApplied.Add(key);
            }
            
            if (time < max) {
                return 0;
            } else {
                if (apply) {
                    keyTimes.Remove(key);
                    keyTimesApplied.Add(key);
                }
                return 2;
            }
        }
        
        public static void Hold(this CodeInputAll key, double time = 0d) {
            int timed = Timed(key, time);
            if (timed < 2 && time > 0d) {
                tmpKeyHold.Key = key;
                tmpKeyHold.Duration = time;
                if (!keysHold.Contains(tmpKeyHold)) {
                    keysHold.Add(new Sequential_Key_Duration() {
                       Key = key,
                       Duration = time 
                    });
                }
            }
            if (timed != 0) {
                return;
            }
            FakeInputHelper.Overrides[key] = FezButtonState.Pressed;
        }
        
        public static void KeepHolding(this CodeInputAll key, double time = 0d) {
            if (!FakeInputHelper.PressedOverrides.Contains(key)) {
                //Only keep holding when already pressed.
                return;
            }
            int timed = Timed(key, time);
            if (timed < 2 && time > 0d) {
                //TODO decide whether to add to a list of KeepHolding keys (sequential input) or not
            }
            if (Timed(key, time) != 0) {
                return;
            }
            FakeInputHelper.Overrides[key] = FezButtonState.Pressed;
        }
        
        public static void Press(this CodeInputAll key) {
            if (FakeInputHelper.PressedOverrides.Contains(key)) {
                //Wait until released
                //add to    -> IM; rov   -> FIH, accessible  -> FIH + IM        -> FIH, acc.-> FIH + IM
                //Overrides -> Set Value -> PressedOverrides -> (down not here) -> Released -> RemovedOverrides
                //Released overrides were in the "Released" state already but need to be removed from Overrides
                //It's the perfect time to resume pressing, but... what about the "first press" when mashing?
                return;
            }
            FakeInputHelper.Overrides[key] = FezButtonState.Pressed;
        }
        
    }
}

