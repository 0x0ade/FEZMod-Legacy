using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using FezGame.Components;
using FezEngine.Structure.Input;
using FezGame.Mod;
using FezEngine.Mod;
using FezEngine.Structure;

namespace FezGame.Speedrun.BOT.Levels {
    public class BOT_ELDERS : BOT_LEVEL {
        
        public BOT_ELDERS(BOT bot)
            : base(bot, new string[] {
                "ELDERS"
            }) {
        }
        
        public override void Update(GameTime gameTime) {
            //spam all 3 keys required. duh.
            CodeInputAll.CancelTalk.Press();//Hexahedron is not a NPC
            CodeInputAll.RotateLeft.Press();
            CodeInputAll.RotateRight.Press();
            //for the swag
            CodeInputAll.Down.Press();
        }
        
    }
}