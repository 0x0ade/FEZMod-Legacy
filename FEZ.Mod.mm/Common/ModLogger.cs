using System;
using System.IO;

namespace Common {
    public class ModLogger {
        private static readonly string LogFilePath = "JAFM Log.txt";//Creates the .txt to where the game was run (next to FEZ.exe)
        private const string TimeFormat = "HH:mm:ss.fff";

        public static void Log(string component, string message) {
            string line = "(" + DateTime.Now.ToString("HH:mm:ss.fff") + ") [" + component + "] " + message;
            Console.WriteLine(line);
            using (FileStream fileStream = File.Open(LogFilePath, FileMode.Append)) {
                using (StreamWriter streamWriter = new StreamWriter(fileStream)) {
                    streamWriter.WriteLine(line);
                }
            }
        }

        public static void Clear() {
            if (File.Exists(LogFilePath)) {
                File.Delete(LogFilePath);
            }
        }

    }
}

