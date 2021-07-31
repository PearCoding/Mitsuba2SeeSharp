using System;

namespace Mitsuba2SeeSharp
{
    public static class Log
    {
        public static void Info(string msg)
        {
            Console.WriteLine("[Info   ] " + msg);
        }

        public static void Warning(string msg)
        {
            var previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[Warning] " + msg);
            Console.ForegroundColor = previous;
        }

        public static void Error(string msg)
        {
            var previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[Error  ] " + msg);
            Console.ForegroundColor = previous;
        }
    };
}
