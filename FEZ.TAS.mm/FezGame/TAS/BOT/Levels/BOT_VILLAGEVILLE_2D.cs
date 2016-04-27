using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using FezGame.Components;
using FezEngine.Structure.Input;
using FezGame.Mod;
using FezEngine.Mod;
using FezEngine.Structure;

namespace FezGame.TAS.BOT.Levels {
    public class BOT_VILLAGEVILLE_2D : BOT_LEVEL {
        
        public int LandedTime;
        public bool LandedWasGrounded;
        public double ClimbedNextToLadder;
        public bool ClimbWasJumping;
        public int ClimbJumpedTime;
        public bool ChestCanJumpToDeath = true;
        
        public BOT_VILLAGEVILLE_2D(BOT bot)
            : base(bot, new string[] {
                "VILLAGEVILLE_2D"
            }) {
        }
        
        public override void Update(GameTime gameTime) {
            if (!BOT.TAS.PlayerManager.CanControl || BOT.TAS.PlayerManager.Action == ActionType.ExitDoor) {
                //wait until player can control Gomez
                return;
            }
            
            //grounding code
            if (Time > 0.2d && BOT.TAS.PlayerManager.Grounded & !LandedWasGrounded) {
                LandedTime++;
            }
            LandedWasGrounded = BOT.TAS.PlayerManager.Grounded;
            
            //jump in front of Gomez's door
            if (Time < 0.4d) {
                CodeInputAll.Right.Hold();
                CodeInputAll.Jump.Hold();
            }
            //climb on top of Gomez's house
            if (LandedTime == 0) {
                //TODO fix CanClimb() for ledges
                CodeInputAll.Left.Press();
            }
            
            //Jumping to the house next to the ladder, with the one platform in between
            if (Time > 0.4d && 1 <= LandedTime && LandedTime <= 2) {
                CodeInputAll.Right.Hold();
                if (BOT.TAS.PlayerManager.Grounded) {
                    CodeInputAll.Jump.Press();
                }
                CodeInputAll.Jump.KeepHolding(0.2d);
            }
            //Climbing the house with the ladder
            if (LandedTime == 2) {
                CodeInputAll.Up.Press();
                ClimbedNextToLadder = Time;
                return;
            }
            
            //the house above the house with the ladder
            if (LandedTime == 3 && BOT.Delta(Time, ClimbedNextToLadder) < 0.5d) {
                CodeInputAll.Jump.Hold();
                CodeInputAll.Up.Press();
            }
            
            //going to the vines
            if (LandedTime == 4) {
                if (BOT.TAS.PlayerManager.Position.X < 22.85f) {
                    //TODO for this, use position instead
                    CodeInputAll.Right.Hold();
                    CodeInputAll.Jump.Hold();
                    return;
                }
                if (BOT.TAS.PlayerManager.Position.Y >= 34f) {
                    CodeInputAll.Up.Press ();
                    LandedTime++;
                }
                return;
            }
            
            if (LandedTime == 5) {
                if (BOT.TAS.PlayerManager.Action == ActionType.Falling) {
                    CodeInputAll.Up.Press();
                } else if (BOT.TAS.PlayerManager.Action == ActionType.Jumping) {
                    CodeInputAll.Jump.KeepHolding();
                    if (!ClimbWasJumping) {
                        ClimbJumpedTime++;
                    }
                    ClimbWasJumping = true;
                } else {
                    CodeInputAll.Jump.Press();
                    ClimbWasJumping = false;
                }
                if (ClimbJumpedTime == 3) {
                    CodeInputAll.Left.Hold();
                }
            }

            //move to the left (ledge to chest). Grab the corner to start longjump sequence with a jump to avoid grabbing cutscene
            if (LandedTime == 6) {
                if (BOT.TAS.PlayerManager.Action.IsOnLedge()) {
                    LandedTime++;
                    //don't return to instantly go to the LandedTime == 6 branch
                } else {
                    //instead else so the other branches don't run
                    if (20f < BOT.TAS.PlayerManager.Position.X) {
                        CodeInputAll.Left.Hold();
                        return;
                    }
                    if (18.2f < BOT.TAS.PlayerManager.Position.X && BOT.TAS.PlayerManager.Position.X <= 20f) {
                        if (BOT.TAS.PlayerManager.Grounded)
                            CodeInputAll.Jump.Press();
                        CodeInputAll.Left.Hold();
                        return;
                    }
                    if (16.5f < BOT.TAS.PlayerManager.Position.X && BOT.TAS.PlayerManager.Position.X <= 18.2f) {
                        CodeInputAll.Right.Hold();
                        return;
                    }
                }
            }
            
            // Longjump sequence after the ledge is grabbed
            if (LandedTime == 7) {
                BOT.LongCliffjump(0.15d);
            }
            
            // Open chest and leave the platform
            if (LandedTime == 8) {
                if (BOT.TAS.PlayerManager.Action == ActionType.OpeningTreasure) {
                    LandedTime++;
                } else {
                    if (BOT.TAS.PlayerManager.Position.X > 7f) {
                        CodeInputAll.Left.Hold();//For when BOT jumps too short on 0x0ade's PC (thanks FPSus)
                    }
                    if (BOT.TAS.PlayerManager.Position.X < 7f) {
                        CodeInputAll.Right.Hold();//For when BOT jumps too far on someone else's PC (thanks FPSus)
                    }
                    CodeInputAll.GrabThrow.Press();
                }
            }
            if (LandedTime == 9) {
                if (BOT.TAS.PlayerManager.Grounded) {
                    CodeInputAll.Right.Hold();
                } else {
                    CodeInputAll.Jump.Hold();
                }
            }
            
            //on the wooden platform down-right to the chest island
            if (LandedTime == 10) {
                CodeInputAll.Right.Hold();
                if (21f < BOT.TAS.PlayerManager.Position.X) {
                    CodeInputAll.Jump.Hold();
                }
            }
            
            //on the boiler thing right to the previous thing (selfnote: naming conventions)
            if (LandedTime == 11) {
                if (BOT.TAS.PlayerManager.Action.IsOnLedge ()) {
                    BOT.CornerKick();
                } else {
                    if (BOT.TAS.PlayerManager.Position.X < 26f) {
                        CodeInputAll.Right.Hold ();
                        return;
                    }
                    if (BOT.TAS.PlayerManager.Position.X >= 26f && BOT.TAS.PlayerManager.Position.X < 26.9f) {
                        CodeInputAll.Right.Hold ();
                        CodeInputAll.Jump.Hold (0.5);
                        return;
                    }
                    if (BOT.TAS.PlayerManager.Position.X >= 26.9f && !BOT.TAS.PlayerManager.Action.IsOnLedge ()) {
                        CodeInputAll.Left.Press ();
                        return;
                    }
                }
            }
            
            //this stays at the end because reasons
            BOT.CancelTalk();
        }
        
    }
}