using System;
using System.Diagnostics;

namespace RTSFramework.Core.Utilities
{
    public static class ConsoleStopWatchTracker
    {
        public static void ReportNeededTimeOnConsole(Action action, string actionName)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            action();
            stopwatch.Stop();
            Console.WriteLine($"{actionName} took {stopwatch.Elapsed.TotalSeconds} Seconds");
        }
    }
}