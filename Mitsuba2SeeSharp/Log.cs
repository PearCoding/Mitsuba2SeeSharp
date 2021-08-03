using System;

namespace Mitsuba2SeeSharp {
    public static class Log {
        public static bool Verbose { get; internal set; }

        public static void Info(string msg) {
            if (Verbose) Console.WriteLine("[Info   ] " + msg);
        }

        public static void Warning(string msg) {
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[Warning] " + msg);
            Console.ForegroundColor = previous;
        }

        public static void Error(string msg) {
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[Error  ] " + msg);
            Console.ForegroundColor = previous;
        }
    }
}
