using System;
using FezGame.Mod;

namespace FezGame {
    public class patch_Fez {

        public void OnExiting(Object sender, EventArgs args) {
            //It's possible that FEZ doesn't contain this method and thus orig_OnExiting won't exist.
            FEZMod.Exit();
        }

    }
}

