using System;

namespace Cewe2pdf {

    class Log {
        public enum Level {
            None,
            Message,
            Error,
            Warning,
            Info,
        };

        public static Level level = Level.Info;

        private static string log = "\nPlease attach this file to bug reports.\n\n";

        public static void Message(string message, bool keepLine = false) {
            log += "[Message] " + message + (keepLine ? "" : "\n");
            if ((int)level >= (int)Level.Message) {
                Console.ForegroundColor = ConsoleColor.White;
                if (keepLine)
                    Console.Write(message);
                else
                    Console.WriteLine(message);
            }
        }

        public static void Error(string message) {
            log += "[Error] " + message + "\n";
            Console.ForegroundColor = ConsoleColor.Red;
            if ((int)level >= (int)Level.Error) Console.WriteLine("[Error]   " + message);
            Console.ForegroundColor = ConsoleColor.White;
            writeLogFile();
        }

        public static void Warning(string message) {
            log += "[Warning] " + message + "\n";
            Console.ForegroundColor = ConsoleColor.Yellow;
            if ((int)level >= (int)Level.Warning) Console.WriteLine("[Warning] " + message);
            Console.ForegroundColor = ConsoleColor.White;
            writeLogFile();
        }

        public static void Info(string message) {
            log += "[Info] " + message + "\n";
            Console.ForegroundColor = ConsoleColor.Gray;
            if ((int)level >= (int)Level.Info) Console.WriteLine("[Info]    " + message);
            Console.ForegroundColor = ConsoleColor.White;
            writeLogFile();
        }

        public static void writeLogFile() {
            System.IO.File.WriteAllText(@"cewe2pdf.log", log);
        }
    }
}
