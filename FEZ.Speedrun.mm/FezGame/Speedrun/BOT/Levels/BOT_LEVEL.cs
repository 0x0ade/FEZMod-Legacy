using Microsoft.Xna.Framework;

namespace FezGame.Speedrun.BOT.Levels {
    public abstract class BOT_LEVEL {
        
        public string[] Levels;
        public BOT BOT;
        public int Time;
        
        public BOT_LEVEL(BOT bot, string[] levels) {
            BOT = bot;
            Levels = levels;
        }
        
        public abstract void Update(GameTime gameTime);
        
    }
}