#pragma warning disable 436
using System;
using MonoMod;
using Microsoft.Xna.Framework.Net;

namespace Microsoft.Xna.Framework.GamerServices {

    [MonoModIgnore]
    public class GamerServicesComponent {
        internal static LocalNetworkGamer LocalNetworkGamer { get; set; }
    }

    public static class Guide {

        [MonoModReplace]
        public static void ShowSignIn(int paneCount, bool onlineOnly) {
            if (paneCount != 1 && paneCount != 2 && paneCount != 4) {
                new ArgumentException("paneCount Can only be 1, 2 or 4 on Windows");
                return;
            }

            if (GamerServicesComponent.LocalNetworkGamer == null) {
                GamerServicesComponent.LocalNetworkGamer = new LocalNetworkGamer();
            }
        }

        [MonoModReplace]
        internal static void Initialise(Game game) {
        }
    }
}