using System;
using System.Threading;
using FezGame.Mod;
using FezEngine.Mod;
using FezGame.Droid;
using System.Globalization;
using System.Reflection;

namespace FezGame {
	public static class Program {

		public static extern void orig_Main(string[] args);
		public static void Main(string[] args) {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            try {
                FEZMod.PreInitialize(args);
            } catch (Exception e) {
                ModLogger.Log("FEZMod", "Handling FEZMod PreInitialize crash...");
                log(e);
                FEZMod.HandleCrash(e);
                throw;
            }

			ModLogger.Log("FEZMod", "Passing to FEZ...");
			orig_Main(args);
		}

        private static extern void orig_MainInternal();
        private static void MainInternal() {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            try {
                orig_MainInternal();
            } catch (Exception e) {
                ModLogger.Log("FEZMod", "Handling FEZ crash...");
                log(e);
                FEZMod.HandleCrash(e);
                throw;
            }
        }

        private static void log(Exception e, string tag = null) {
            for (Exception e_ = e; e_ != null; e_ = e_.InnerException) {
                ModLogger.Log("FEZMod" + (tag == null ? "" : ("[" + tag + "]")), e_.GetType().FullName + ": " + e_.Message + "\n" + e_.StackTrace);
                if (e_ is ReflectionTypeLoadException) {
                    ReflectionTypeLoadException rtle = (ReflectionTypeLoadException) e_;
                    for (int i = 0; i < rtle.Types.Length; i++) {
                        ModLogger.Log("FEZMod" + (tag == null ? "" : ("[" + tag + "]")), "ReflectionTypeLoadException.Types["+i+"]: " + rtle.Types[i]);
                    }
                    for (int i = 0; i < rtle.LoaderExceptions.Length; i++) {
                            log(rtle.LoaderExceptions[i], tag + (tag == null ? "" : ", ") + "rtle:" + i);
                    }
                }
                if (e_ is TypeLoadException) {
                    ModLogger.Log("FEZMod" + (tag == null ? "" : ("[" + tag + "]")), "TypeLoadException.TypeName: " + ((TypeLoadException) e_).TypeName);
                }
                if (e_ is BadImageFormatException) {
                    ModLogger.Log("FEZMod" + (tag == null ? "" : ("[" + tag + "]")), "BadImageFormatException.FileName: " + ((BadImageFormatException) e_).FileName);
                }
            }
        }

	}
}

