using System;
using FezEngine.Structure;
using FezGame;
using Microsoft.Xna.Framework;

namespace FezGame.Components {
    public class DotHost {

        public SoundEmitter eIdle;

        public void orig_Draw(GameTime gameTime) {
        }

        public void Draw(GameTime gameTime) {
            //Fixes NPE as eIdle may be null.
            if (Fez.LongScreenshot) {
                if (this.eIdle != null) {
                    eIdle.VolumeFactor = 0f;
                }
                return;
            }
            orig_Draw(gameTime);
        }

    }
}

