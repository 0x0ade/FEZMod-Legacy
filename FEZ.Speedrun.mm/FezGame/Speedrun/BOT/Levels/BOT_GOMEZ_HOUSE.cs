using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using FezGame.Components;
using FezEngine.Structure.Input;
using FezGame.Mod;
using FezEngine.Structure;

namespace FezGame.Speedrun.BOT.Levels {
    public static class BOT_GOMEZ_HOUSE {
        public static void Update(BOT BOT, GameTime gameTime) {
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