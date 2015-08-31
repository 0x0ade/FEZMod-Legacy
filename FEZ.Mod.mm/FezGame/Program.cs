using System;
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
                for (Exception e_ = e; e_ != null; e_ = e_.InnerException) {
                    ModLogger.Log("FEZMod", e_.ToString());
                }
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
                for (Exception e_ = e; e_ != null; e_ = e_.InnerException) {
                    ModLogger.Log("FEZMod", e_.ToString());
                }
                FEZMod.HandleCrash(e);
            }
        }

	}
}

