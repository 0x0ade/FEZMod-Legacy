using System;
using FezGame.Mod;
using FezGame.Mod;

namespace FezGame {
	public class Program {

		public static void orig_Main(string[] args) {
		}

		public static void Main(string[] args) {
            FEZMod.PreInitialize(args);

			ModLogger.Log("FEZMod", "Passing to FEZ...");
			orig_Main(args);
		}

        private static void orig_MainInternal() {
        }

        private static void MainInternal() {
            try {
                orig_MainInternal();
            } catch (Exception e) {
                ModLogger.Log("FEZMod", "Handling crash...");
                ModLogger.Log("FEZMod", e.ToString());
                FEZMod.HandleCrash(e);
            }
        }

	}
}

