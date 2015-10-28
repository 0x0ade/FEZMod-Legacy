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
        
        public BOT(TASComponent tas) {
            TAS = tas;
        }
        
        public void Update(GameTime gameTime) {
            /*
            Gomez's house
            Every house is the same:
            - go right until door (volume n (2D: 1))
            - FakeInputHelper.Press up
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
                    FakeInputHelper.Hold(CodeInput.Right);
                    return;
                }
                
                //FakeInputHelper.Press up as soon as Gomez is grounded (may be falling from bed)
                if (TAS.PlayerManager.Grounded) {
                    FakeInputHelper.Press(CodeInput.Up);
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
                    FakeInputHelper.Hold(CodeInput.Jump);
                }
                //climb on top of Gomez's house
                if (villageTime < 0.6d) {
                    //TODO fix CanClimb() for ledges. Currently even untested for ladders and vines.
                    FakeInputHelper.Press(CodeInput.Up);
                }
                
                //grounding code
                if (villageTime > 0.2d && TAS.PlayerManager.Grounded & !villageLandedWasGrounded) {
                    villageLandedTime++;
                }
                villageLandedWasGrounded = TAS.PlayerManager.Grounded;
                
                //Jumping to the house next to the ladder, with the one platform in between
                if (villageTime > 0.4d && 1 <= villageLandedTime && villageLandedTime <= 2) {
                    FakeInputHelper.Hold(CodeInput.Right);
                    if (TAS.PlayerManager.Grounded) {
                        FakeInputHelper.Hold(CodeInput.Jump, 0.5d);
                    }
                    FakeInputHelper.KeepHolding(CodeInput.Jump, 0.5d);
                }
                //Climbing the house with the ladder
                if (villageLandedTime == 2) {
                    FakeInputHelper.Press(CodeInput.Up);
                    villageClimbedNextToLadder = villageTime;
                    return;
                }
                
                //the house above the house with the ladder
                if (3 == villageLandedTime && Delta(villageTime, villageClimbedNextToLadder) < 0.5d) {
                    FakeInputHelper.Hold(CodeInput.Jump);
                    FakeInputHelper.Press(CodeInput.Up);
                }
                
                //going to the vines
                if (4 == villageLandedTime && Delta(villageTime, villageClimbedNextToLadder) < 1.47d) {
                    //TODO for this, use position instead
                    FakeInputHelper.Hold(CodeInput.Right);
                    FakeInputHelper.Hold(CodeInput.Jump);
                    return;
                }
                //climbing up the vines
                if (4 == villageLandedTime) {
                    if (TAS.PlayerManager.Action == ActionType.Falling) {
                        FakeInputHelper.Press(CodeInput.Up);
                    } else if (TAS.PlayerManager.Action == ActionType.Jumping) {
                        FakeInputHelper.KeepHolding(CodeInput.Jump);
                        if (!villageClimbWasJumping) {
                            villageClimbJumpedTime++;
                        }
                        villageClimbWasJumping = true;
                    } else {
                        FakeInputHelper.Press(CodeInput.Jump);
                        villageClimbWasJumping = false;
                    }
                    if (villageClimbJumpedTime == 4) {
                        FakeInputHelper.Hold(CodeInput.Left);
                    }
                    return;
                }
                
                //move to the left (ledge to chest)
                if (5 == villageLandedTime && TAS.PlayerManager.Position.X > 17.5f) {
                    FakeInputHelper.Hold(CodeInput.Left);
                    return;
                }
                //climb down
                if (5 == villageLandedTime && TAS.PlayerManager.Position.X <= 17.5f && Delta(villageTime, villageChestCanJumpToDeath) <= 0d) {
                    FakeInputHelper.Press(CodeInput.Down);
                    if (TAS.PlayerManager.Animation.Timing.Ended) {
                        //TODO which animation?
                        villageChestCanJumpToDeath = villageTime;
                    }
                    FakeInputHelper.Hold(CodeInput.Left);
                    return;
                }
                //wait until jumping to death (store respawn information)
                if (5 == villageLandedTime && !villageChestJumpedToDeath && Delta(villageTime, villageChestCanJumpToDeath) >= 0.05d) {
                    FakeInputHelper.Press(CodeInput.Jump);
                    FakeInputHelper.Hold(CodeInput.Left);
                    villageChestJumpedToDeath = true;
                    return;
                }
                //FakeInputHelper.Hold left and jump frame-perfectly
                if (villageChestJumpedToDeath && TAS.PlayerManager.LastAction == ActionType.Dying) {
                    //TODO time the jump
                    FakeInputHelper.Press(CodeInput.Jump);
                    FakeInputHelper.Hold(CodeInput.Left);
                    return;
                }
                if (!villageChestJumpedToDeath) {
                    return;
                }
            }
        }
        
        public double Delta(double oldt, double newt) {
            double d = oldt - newt;
            return d == oldt ? 0d : Math.Max(0d, d);
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

