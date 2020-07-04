using System;
using System.Collections.Generic;
using System.Text;

namespace Cewe2pdf {

    class Log {
        public enum Level {
            None = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
        };

        public static Level level = Level.Info;

        public static void Info(string message) {
            if ((int)level >= (int)Level.Info) Console.WriteLine(message);
        }

        public static void Warning(string message) {
            if ((int)level >= (int)Level.Warning) Console.WriteLine("[Warning] " + message);
        }

        public static void Error(string message) {
            if ((int)level >= (int)Level.Error) Console.WriteLine("[Error] " + message);
        }
    }
}
