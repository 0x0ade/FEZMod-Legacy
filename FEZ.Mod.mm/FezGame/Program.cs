using System;
using FezGame.Mod;
using FezGame.Mod;

namespace FezGame {
	public class Program {

		public static void orig_Main(string[] args) {
		}

		public static void Main(string[] args) {
            FEZMod.PreInitialize(args);

			ModLogger.Log("JAFM", "Passing to FEZ...");
			orig_Main(args);
		}

	}
}

