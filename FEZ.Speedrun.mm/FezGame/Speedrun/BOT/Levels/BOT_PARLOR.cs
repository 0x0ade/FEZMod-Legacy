using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using FezGame.Components;
using FezEngine.Structure.Input;
using FezGame.Mod;
using FezEngine.Structure;

namespace FezGame.Speedrun.BOT.Levels {
    public class BOT_PARLOR : BOT_LEVEL {
        
        public bool parlorCodeInput;
        
        public BOT_PARLOR(BOT bot)
            : base(bot, new string[] {
                "PARLOR"
            }) {
        }
        
        public override void Update(GameTime gameTime) {
            if (!BOT.TAS.PlayerManager.CanControl || BOT.TAS.PlayerManager.Action == ActionType.ExitDoor) {
                //wait until player can control Gomez
                return;
            }
            
            if (!parlorCodeInput) {
                FakeInputHelper.Sequences.Add(ControllerCodeHelper.MonoclePainting);
                parlorCodeInput = true;
                return;
            }
            
            if (ControllerCodeHelper.MonoclePainting.Current == 0 /*finished, thus reset*/ && parlorCodeInput) {
                //TODO do stuff in PARLOR
            }
        }
    }
}