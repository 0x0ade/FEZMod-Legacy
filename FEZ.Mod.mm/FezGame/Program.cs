using System;
using FezGame.Mod;
using FezGame.Mod;

namespace FezGame {
	public class Program {

		public static void orig_Main(string[] args) {
		}

		public static void Main(string[] args) {
            try {
                FEZMod.PreInitialize(args);
            } catch (Exception e) {
                ModLogger.Log("FEZMod", "Handling FEZMod PreInitialize crash...");
                ModLogger.Log("FEZMod", e.ToString());
                FEZMod.HandleCrash(e);
            }

			ModLogger.Log("FEZMod", "Passing to FEZ...");
			orig_Main(args);
		}

        private static void orig_MainInternal() {
        }

        private static void MainInternal() {
            try {
                orig_MainInternal();
            } catch (Exception e) {
                ModLogger.Log("FEZMod", "Handling FEZ crash...");
                ModLogger.Log("FEZMod", e.ToString());
                FEZMod.HandleCrash(e);
            }
        }

	}
}

