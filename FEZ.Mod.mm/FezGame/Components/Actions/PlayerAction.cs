using System;
using MonoMod;
using FezGame.Services;

namespace FezGame.Components.Actions {
    [MonoModIgnore]
    public class PlayerAction {

        public IPlayerManager PlayerManager { get { return null; }}

    }
}

