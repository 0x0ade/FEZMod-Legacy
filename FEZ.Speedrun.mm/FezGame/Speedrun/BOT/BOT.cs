using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using FezEngine;
using FezGame.Components;
using FezEngine.Structure.Input;
using FezGame.Mod;
using FezEngine.Structure;
using FezEngine.Services;
using FezEngine.Tools;
using System.Collections.Generic;

namespace FezGame.Speedrun.BOT {
    //Broken optimized TASer.
    public class BOT {
        
        public TASComponent TAS;
        
        public bool gomezHouseDoored;
        public double villageTime;
        public int villageLandedTime;
        public bool villageLandedWasGrounded;
        public double villageClimbedNextToLadder;
        public bool villageClimbWasJumping;
        public int villageClimbJumpedTime;
        public double villageChestCanJumpToDeath;
        public bool villageChestJumpedToDeath;
        
        private GameTime gameTime;
        private Dictionary<CodeInput, double> keyTimes = new Dictionary<CodeInput, double>();
        private List<CodeInput> keyTimesApplied = new List<CodeInput>();
        
        public BOT(TASComponent tas) {
            TAS = tas;
        }
        
        public void Update(GameTime gameTime) {
            this.gameTime = gameTime;
            keyTimesApplied.Clear();
            /*
            if (LevelManager.Name.StartsWith("GOMEZ_HOUSE_END_") && (PlayerManager.Action == ActionType.EnterDoorSpin || PlayerManager.Action == ActionType.EnteringDoor)) {
                clock.Running = false;
                return "ZE_DOOR_AT_ZE_END";
            }
            */
            
            /*
            Gomez's house
            Every house is the same:
            - go right until door (volume n (2D: 1))
            - press up
            */
            if (TAS.LevelManager.Name.StartsWith("GOMEZ_HOUSE_")) {
                //go right until door
                if (!gomezHouseDoored) {
                    foreach (Volume vol in TAS.PlayerManager.CurrentVolumes) {
                        if (vol.Id == 1) {
                            gomezHouseDoored = true;
                            break;
                        }
                    }
                }
                if (!gomezHouseDoored) {
                    Hold(CodeInput.Right);
                    return;
                }
                
                //press up as soon as Gomez is grounded (may be falling from bed)
                if (TAS.PlayerManager.Grounded) {
                    Press(CodeInput.Up);
                }
            }
            
            /*
            Villageville 2D:
            - wait until can move
            - jump up
            */
            else if (TAS.LevelManager.Name == "VILLAGEVILLE_2D") {
                if (!TAS.PlayerManager.CanControl || TAS.PlayerManager.Action == ActionType.ExitDoor) {
                    //wait until player can control Gomez
                    return;
                }
                
                villageTime += gameTime.ElapsedGameTime.TotalSeconds;
                
                //jump in front of Gomez's door
                if (villageTime < 0.5d) {
                    Hold(CodeInput.Jump);
                }
                //climb on top of Gomez's house
                if (villageTime < 0.6d) {
                    //TODO fix CanClimb() for ledges. Currently even untested for ladders and vines.
                    Press(CodeInput.Up);
                }
                
                //grounding code
                if (villageTime > 0.2d && TAS.PlayerManager.Grounded & !villageLandedWasGrounded) {
                    villageLandedTime++;
                }
                villageLandedWasGrounded = TAS.PlayerManager.Grounded;
                
                //Jumping to the house next to the ladder, with the one platform in between
                if (villageTime > 0.4d && 1 <= villageLandedTime && villageLandedTime <= 2) {
                    Hold(CodeInput.Right);
                    if (TAS.PlayerManager.Grounded) {
                        Hold(CodeInput.Jump, 0.5d);
                    }
                    KeepHolding(CodeInput.Jump, 0.5d);
                }
                //Climbing the house with the ladder
                if (villageLandedTime == 2) {
                    Press(CodeInput.Up);
                    villageClimbedNextToLadder = villageTime;
                    return;
                }
                
                //the house above the house with the ladder
                if (3 == villageLandedTime && Delta(villageTime, villageClimbedNextToLadder) < 0.5d) {
                    Hold(CodeInput.Jump);
                    Press(CodeInput.Up);
                }
                
                //going to the vines
                if (4 == villageLandedTime && Delta(villageTime, villageClimbedNextToLadder) < 1.47d) {
                    //TODO for this, use position instead
                    Hold(CodeInput.Right);
                    Hold(CodeInput.Jump);
                    return;
                }
                //climbing up the vines
                if (4 == villageLandedTime) {
                    if (TAS.PlayerManager.Action == ActionType.Falling) {
                        Press(CodeInput.Up);
                    } else if (TAS.PlayerManager.Action == ActionType.Jumping) {
                        KeepHolding(CodeInput.Jump);
                        if (!villageClimbWasJumping) {
                            villageClimbJumpedTime++;
                        }
                        villageClimbWasJumping = true;
                    } else {
                        Press(CodeInput.Jump);
                        villageClimbWasJumping = false;
                    }
                    if (villageClimbJumpedTime == 4) {
                        Hold(CodeInput.Left);
                    }
                    return;
                }
                
                //move to the left (ledge to chest)
                if (5 == villageLandedTime && TAS.PlayerManager.Position.X > 17.5f) {
                    Hold(CodeInput.Left);
                    return;
                }
                //climb down
                if (5 == villageLandedTime && TAS.PlayerManager.Position.X <= 17.5f && Delta(villageTime, villageChestCanJumpToDeath) <= 0d) {
                    Press(CodeInput.Down);
                    if (TAS.PlayerManager.Animation.Timing.Ended) {
                        //TODO which animation?
                        //if it would set it if it ended, it would never leave this branch
                        villageChestCanJumpToDeath = villageTime;
                    }
                    Hold(CodeInput.Left);
                    return;
                }
                //wait until jumping to death (store respawn information)
                if (5 == villageLandedTime && !villageChestJumpedToDeath && Delta(villageTime, villageChestCanJumpToDeath) >= 0.3d) {
                    Press(CodeInput.Jump);
                    Hold(CodeInput.Left);
                    villageChestJumpedToDeath = true;
                    return;
                }
                //hold left and jump frame-perfectly
                //TODO doesn't do the first jump on its own; works after doing the first jump
                if (villageChestJumpedToDeath) {
                    //TODO time the jump
                    Press(CodeInput.Jump);
                    Hold(CodeInput.Left);
                    return;
                }
                if (!villageChestJumpedToDeath) {
                    return;
                }
            }
        }
        
        //FakeInputHelper helpers
        //TODO maybe move some of them to FakeInputHepler?
        
        public double Delta(double oldt, double newt) {
            double d = oldt - newt;
            return d == oldt ? 0d : Math.Max(0d, d);
        }
        
        public bool Timed(CodeInput key, double max, bool apply = true) {
            if (max <= 0d) {
                return true;
            }
            
            double time;
            apply = apply && !keyTimesApplied.Contains(key);
            
            if (!keyTimes.TryGetValue(key, out time)) {
                if (apply) {
                    keyTimes[key] = 0d;
                    keyTimesApplied.Add(key);
                }
                return true;
            }
            
            if (apply) {
                time = keyTimes[key] = time + gameTime.ElapsedGameTime.TotalSeconds;
                keyTimesApplied.Add(key);
            }
            
            if (time < max) {
                return true;
            } else {
                if (apply) {
                    keyTimes.Remove(key);
                    keyTimesApplied.Add(key);
                }
                return false;
            }
        }
        
        public void Hold(CodeInput key, double time = 0d) {
            if (!Timed(key, time)) {
                return;
            }
            FakeInputHelper.Overrides[key] = FezButtonState.Pressed;
        }
        
        public void KeepHolding(CodeInput key, double time = 0d) {
            if (!FakeInputHelper.PressedOverrides.Contains(key)) {
                //Only keep holding when already pressed.
                return;
            }
            if (!Timed(key, time)) {
                return;
            }
            FakeInputHelper.Overrides[key] = FezButtonState.Pressed;
        }
        
        public void Press(CodeInput key) {
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
        
        
        //Logic helpers
        
        /*
        public bool CanClimb() {
            return TAS.PlayerManager.Action.IsOnLedge() ||
                TAS.PlayerManager.Action.IsClimbingLadder() ||
                TAS.PlayerManager.Action.IsClimbingVine() ||
                IsOnClimb();
        }
        
        private bool IsOnClimb() {
            //Decompiled code; hnnnnnnng
            Vector3 vector = TAS.CameraManager.Viewpoint.ForwardVector();
			Vector3 vector2 = TAS.CameraManager.Viewpoint.RightVector();
			float num = float.MaxValue; //3,402823E+38; //?
			bool flag = false;
			TrileInstance trileInstance = null;
            NearestTriles nearestTriles = TAS.LevelManager.NearestTrile(TAS.PlayerManager.Position - 0.002f * Vector3.UnitY);
            bool flag2 = (nearestTriles.Surface != null && nearestTriles.Surface.Trile.ActorSettings.Type == ActorType.Vine);
			PointCollision[] cornerCollision = TAS.PlayerManager.CornerCollision;
			for (int i = 0; i < cornerCollision.Length; i++) {
				PointCollision pointCollision = cornerCollision[i];
				if (pointCollision.Instances.Surface != null && TestClimbCollision(pointCollision.Instances.Surface, true)) {
					TrileInstance surface = pointCollision.Instances.Surface;
					float num2 = surface.Position.Dot(vector);
					if (flag2 && num2 < num && TestClimbCollision(pointCollision.Instances.Surface, true)) {
						num = num2;
						trileInstance = surface;
					}
				}
			}
			foreach (NearestTriles current in TAS.PlayerManager.AxisCollision.Values) {
				if (current.Surface != null && TestClimbCollision(current.Surface, false)) {
					TrileInstance surface2 = current.Surface;
					float num3 = surface2.Position.Dot(vector);
					if (flag2 && num3 < num) {
						flag = true;
						num = num3;
						trileInstance = surface2;
					}
				}
			}
            
            return trileInstance != null;
		}
        
        private bool TestClimbCollision(TrileInstance instance, bool onAxis) {
            //Yet again decompiler code. Hnnnng
			TrileActorSettings actorSettings = instance.Trile.ActorSettings;
			float combinedPhi = FezMath.WrapAngle(actorSettings.Face.ToPhi() + instance.Phi);
			Axis axis = FezMath.AxisFromPhi(combinedPhi);
			return ((actorSettings.Type == ActorType.Vine || actorSettings.Type == ActorType.Ladder) && axis == TAS.CameraManager.Viewpoint.VisibleAxis() == onAxis) || instance.GetRotatedFace(TAS.CameraManager.VisibleOrientation) == CollisionType.TopOnly;
		}
        */
        
    }
}

