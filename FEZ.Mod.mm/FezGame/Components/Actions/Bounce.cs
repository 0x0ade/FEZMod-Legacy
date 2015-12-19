//Fixed in the FNA update / 1.12
#if !FNA
#pragma warning disable 436
using FezGame.Mod;
using FezGame.Structure;
using FezGame.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using MonoMod;

namespace FezGame.Components.Actions {
    public class Bounce : PlayerAction {
        
        private IGameStateManager gameStateManager;
        
        public Bounce(Game game)
            : base(game) {
            //no-op
        }
        
        [MonoModIgnore] public extern void orig_TestConditions();
        public void TestConditions() {
            if (gameStateManager == null) {
                gameStateManager = ServiceHelper.Get<IGameStateManager>();
            }
            if (FEZMod.EnableBugfixes && (gameStateManager.InCutscene || !PlayerManager.CanControl)) {
                if (PlayerManager.Action == ActionType.Bouncing) {
                    PlayerManager.Action = ActionType.Landing;
                }
            } else {
                orig_TestConditions();
            }
        }

        [MonoModIgnore] protected override extern bool IsActionAllowed(ActionType type);
    }
}
#endif
