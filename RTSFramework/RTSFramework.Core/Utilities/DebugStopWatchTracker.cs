﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RTSFramework.Core.Utilities
{
    public static class DebugStopWatchTracker
    {
		public static void ReportNeededTimeOnDebug(Action action, string actionName)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			action();
			stopwatch.Stop();

			//var oldColor = Console.ForegroundColor;
			//Console.ForegroundColor = ConsoleColor.Blue; ;
			Debug.WriteLine($"{actionName} took {stopwatch.Elapsed.TotalSeconds} Seconds");
			//Console.ForegroundColor = oldColor;
		}

		public static async Task ReportNeededTimeOnDebug(Task action, string actionName)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			await action;
			stopwatch.Stop();

			//var oldColor = Console.ForegroundColor;
			//Console.ForegroundColor = ConsoleColor.Blue; ;
			Debug.WriteLine($"{actionName} took {stopwatch.Elapsed.TotalSeconds} Seconds");
			//Console.ForegroundColor = oldColor;
		}
	}
}