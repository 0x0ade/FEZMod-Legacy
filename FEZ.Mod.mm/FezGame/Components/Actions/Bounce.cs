﻿using System;
using FezGame;
using FezGame.Mod;
using FezGame.Structure;
using FezGame.Services;
using FezEngine.Tools;

namespace FezGame.Components.Actions {
    public class Bounce : PlayerAction {

        public void orig_TestConditions() {
        }

        public void TestConditions() {
            if (FEZMod.EnableBugfixes && (ServiceHelper.Get<IGameStateManager>().InCutscene || !PlayerManager.CanControl)) {
                if (PlayerManager.Action == ActionType.Bouncing) {
                    PlayerManager.Action = ActionType.Landing;
                }
            } else {
                orig_TestConditions();
            }
        }

    }
}
