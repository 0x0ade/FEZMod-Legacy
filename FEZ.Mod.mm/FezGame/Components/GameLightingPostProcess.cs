using Common;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using MonoMod;
using FezEngine.Mod;
using System.Reflection;
using FezEngine.Components;
using FezGame.Services;
using FezEngine;

namespace FezGame.Components {
    public class patch_GameLightingPostProcess : GameLightingPostProcess {

        public patch_GameLightingPostProcess(Game game)
            : base(game) {
            //no-op
        }

        private IPlayerManager playerManager;
        protected extern void orig_DoSetup();
        protected void DoSetup() {
            //TODO use MonoModHelper.DisableMakePublic(); and use the shared PlayerManager
            if (playerManager == null) {
                playerManager = ServiceHelper.Get<IPlayerManager>();
                if (playerManager.Mesh == null) {
                    ModLogger.Log("FEZMod", "FEZMod hates everyone and rage-killed GomezHost.");
                    ModLogger.Log("FEZMod", "Actually, it loves everyone and thus needs to pre-init GomezHost.");
                    if (GomezHost.Instance == null) {
                        ModLogger.Log("FEZMod", "But something deep inside hugged GomezHost to death. RIP.");
                        throw new Exception("Dàf úck Wänt wron");
                    }
                }
            }

            orig_DoSetup();
        }

    }
}

