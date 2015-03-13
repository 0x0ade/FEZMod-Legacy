using System;
using FezGame.Mod;

namespace FezGame {
    public class patch_Fez {

        public void orig_Exit() {
        }

        public void Exit() {
            orig_Exit();
            FEZMod.Exit();
        }

    }
}

