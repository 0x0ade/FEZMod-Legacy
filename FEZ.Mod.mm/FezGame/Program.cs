using System;
using FezGame;
using FezEngine.Tools;
using FezGame.Mod;

namespace FezGame {
	public class Program {

		public static void orig_Main(String[] args) {
		}

		public static void Main(String[] args) {
            FEZMod.Initialize();

			Console.WriteLine("Checking for custom arguments...");
			for (int i = 0; i < args.Length; i++) {
				if ((args[i] == "-l" || args[i] == "--load-level") && i+1 < args.Length) {
					Console.WriteLine("Found -l / --load-level: "+args[i+1]);
					Fez.ForcedLevelName = args[i+1];
					//Fez.SkipLogos = true;
					Fez.SkipIntro = true;
				}
				if (args[i] == "-lc" || args[i] == "--level-chooser") {
					Console.WriteLine("Found -lc / --level-chooser");
					Fez.LevelChooser = true;
					//Fez.SkipLogos = true;
					Fez.SkipIntro = true;
				}
				if (args[i] == "-ls" || args[i] == "--long-screenshot") {
					if (i+1 < args.Length && !args[i+1].StartsWith("-")) {
						Console.WriteLine("Found -ls / --long-screenshot: "+args[i+1]);
						Fez.ForcedLevelName = args[i+1];
					} else {
						Console.WriteLine("Found -ls / --long-screenshot");
					}
					Fez.LongScreenshot = true;
					Fez.DoubleRotations = true;
					//Fez.SkipLogos = true;
					Fez.SkipIntro = true;
				}
				if (args[i] == "-d" || args[i] == "--dump") {
					Console.WriteLine("Found -d / --dump");
					MemoryContentManager.AssetExists("JAFM_DUMP_WORKAROUND");
				}
				if (args[i] == "-da" || args[i] == "--dump-all") {
					Console.WriteLine("Found -da / --dump-all");
					MemoryContentManager.AssetExists("JAFM_DUMPALL_WORKAROUND");
				}
			}

			Console.WriteLine("Passing to FEZ...");
			orig_Main(args);
		}

	}
}

