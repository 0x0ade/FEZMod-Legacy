using System;

namespace FezGame.Mod {
    public class Logger {

        public static void orig_Log(string component, LogSeverity severity, string message) {
        }

        public static void Log(string component, LogSeverity severity, string message) {
            Console.WriteLine("(" + DateTime.Now.ToString("HH:mm:ss.fff") + ") [" + component + "] " + message);
            orig_Log(component, severity, message);
        }

    }
}

