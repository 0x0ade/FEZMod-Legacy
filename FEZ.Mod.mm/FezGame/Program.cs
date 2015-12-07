using System;
using FezGame.Mod;
using FezEngine.Mod;
using FezGame.Droid;

namespace FezGame {
	public static class Program {

		public static extern void orig_Main(string[] args);
		public static void Main(string[] args) {
            try {
                FEZMod.PreInitialize(args);
            } catch (Exception e) {
                ModLogger.Log("FEZMod", "Handling FEZMod PreInitialize crash...");
                for (Exception e_ = e; e_ != null; e_ = e_.InnerException) {
                    ModLogger.Log("FEZMod", e_.GetType().FullName + ": " + e_.Message + "\n" + e_.StackTrace);
                }
                FEZMod.HandleCrash(e);
                throw e;
            }

			ModLogger.Log("FEZMod", "Passing to FEZ...");
			orig_Main(args);
		}

        private static extern void orig_MainInternal();
        private static void MainInternal() {
            try {
                orig_MainInternal();
            } catch (Exception e) {
                ModLogger.Log("FEZMod", "Handling FEZ crash...");
                if (!FezDroid.InAndroid) {
                    for (Exception e_ = e; e_ != null; e_ = e_.InnerException) {
                        ModLogger.Log("FEZMod", e_.ToString());
                    }
                } else {
                    for (Exception e_ = e; e_ != null; e_ = e_.InnerException) {
                        ModLogger.Log("FEZMod", e_.GetType().FullName + ": " + e_.Message + "\n" + e_.StackTrace);
                    }
                }
                FEZMod.HandleCrash(e);
                throw e;
            }
        }

	}
}

