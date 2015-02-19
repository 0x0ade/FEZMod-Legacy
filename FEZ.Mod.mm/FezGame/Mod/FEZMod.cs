using System;
using FezGame;

namespace FezGame.Mod {
    public static class FEZMod {
        public static string Version = "0.0.4";

        public static void Initialize() {
            Console.WriteLine("JustAnotherFEZMod (JAFM) "+FEZMod.Version);

            Fez.Version = FEZMod.Version;
            Fez.Version += " (JustAnotherFEZMod)";

            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        }

    }
}

