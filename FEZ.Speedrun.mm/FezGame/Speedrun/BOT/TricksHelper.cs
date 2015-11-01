using FezEngine.Structure.Input;
using FezGame.Mod;
using FezGame.Components;
using FezEngine;
using FezGame.Structure;

namespace FezGame.Speedrun.BOT {
	/*
	 * Sequences of inputs made to ease life when we want to use corner jumps, corner kicks, TP jumps and stuff like that
	 */
	public static class TricksHelper {
        
        public static void CornerKick(this BOT BOT) {
            CornerKick(BOT.TAS);
        }
        public static void CornerKick(TASComponent TAS) {
            (TAS.PlayerManager.LookingDirection == HorizontalDirection.Right ? CodeInputAll.Left : CodeInputAll.Right)
                .CornerKick();
        }
        public static void CornerKick(this CodeInputAll dir) {
            dir.Hold();
            CodeInputAll.Jump.Press();
        }
        
        
        private static bool longCliffjumpShouldJump;
        private static CodeInputAll longCliffjumpDir;
        public static void LongCliffjump(this BOT BOT, double time) {
            LongCliffjump(BOT.TAS, time);
        }
        public static void LongCliffjump(TASComponent TAS, double time) {
            //wait until on the ledge, then find out which direction
            if (!longCliffjumpShouldJump && TAS.PlayerManager.Action.IsOnLedge()) {
                longCliffjumpDir = (TAS.PlayerManager.LookingDirection == HorizontalDirection.Right ? CodeInputAll.Left : CodeInputAll.Right);
                longCliffjumpShouldJump = true;
            }
            longCliffjumpDir.Hold();
            
            //jump once
            if (longCliffjumpShouldJump) {
                CodeInputAll.Jump.Press();
                longCliffjumpShouldJump = false;
            }
            
            //hold left and jump frame-perfectly
            if (TAS.PlayerManager.LastAction == ActionType.FreeFalling) {
                //FIXME Results differ between runs, most probably due to the FPS on 0x0ade's PC...
                CodeInputAll.Jump.Hold(time);
            }
        }
        
	}
}

