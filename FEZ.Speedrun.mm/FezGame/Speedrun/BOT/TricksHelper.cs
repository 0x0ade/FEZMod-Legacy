using FezEngine.Structure.Input;
using FezGame.Mod;
using FezGame.Components;
using FezEngine;

namespace FezGame.Speedrun.BOT {
	/*
	 * Sequences of inputs made to ease life when we want to use corner jumps, corner kicks, TP jumps and stuff like that
	 */
	public static class TricksHelper {
		
        public static void CornerKick(this BOT BOT) {
            CornerKick(BOT.TAS);
        }
        
        public static void CornerKick(TASComponent TAS) {
            if (TAS.PlayerManager.LookingDirection == HorizontalDirection.Right) {
                CornerKickLeft();
            } else {
                CornerKickRight();
            }
        }
        
        public static void CornerKickLeft() {
            CodeInputAll.Left.Hold();
            CodeInputAll.Jump.Press();
        }
        
        public static void CornerKickRight() {
            CodeInputAll.Right.Hold();
            CodeInputAll.Jump.Press();
        }
        
	}
}

