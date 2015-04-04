using System;
using System.Collections.Generic;
using FezGame.Mod;

namespace FezGame {
    public class patch_Fez : Fez {

        public static void orig_LoadComponents(Fez game) {
        }

        public static void LoadComponents(Fez game) {
            orig_LoadComponents(game);
            FEZMod.LoadComponents(game);
        }

    }
}

