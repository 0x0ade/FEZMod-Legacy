using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using FezGame.Components;
using FezEngine.Structure.Input;
using FezGame.Mod;
using FezEngine.Mod;
using FezEngine.Structure;

namespace FezGame.Speedrun.BOT.Levels {
    public class BOT_GOMEZ_HOUSE : BOT_LEVEL {
        
        public BOT_GOMEZ_HOUSE(BOT bot)
            : base(bot, new string[] {
                "GOMEZ_HOUSE_2D",
                "GOMEZ_HOUSE",
                "GOMEZ_HOUSE_END_32",
                "GOMEZ_HOUSE_END_64"
            }) {
        }
        
        public override void Update(GameTime gameTime) {
            //go right until door
            bool gomezHouseDoored = false;
            foreach (Volume vol in BOT.TAS.PlayerManager.CurrentVolumes) {
                if (vol.Id == 1) {//TODO ID may differ between houses
                    gomezHouseDoored = true;
                    break;
                }
            }
            
            if (!gomezHouseDoored) {
                CodeInputAll.Right.Hold();
            } else if (BOT.TAS.PlayerManager.Grounded) {
                //press up as soon as Gomez is grounded (may be falling from bed)
                CodeInputAll.Up.Press();
            }
        }
        
    }
}