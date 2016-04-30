using Microsoft.Xna.Framework;

namespace FezGame.Components {
    public class patch_GameLevelHost {

        public extern void orig_DoFullDraw(
#if FNA
            GameTime gameTime
#endif
        );
        public void DoFullDraw(
#if FNA
            GameTime gameTime
#endif
        ) {
            orig_DoFullDraw(
#if FNA
            gameTime
#endif
            );
            if (SlaveGomezHost.Instance != null) {
                SlaveGomezHost.Instance.DoDraw();
            }
        }

    }
}

