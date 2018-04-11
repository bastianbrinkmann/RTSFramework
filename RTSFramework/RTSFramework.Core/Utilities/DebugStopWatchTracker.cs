using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RTSFramework.Core.Utilities
{
    public static class DebugStopWatchTracker
    {
		public static T ReportNeededTimeOnDebug<T>(Func<T> action, string actionName)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			T result = action();
			stopwatch.Stop();

			Debug.WriteLine($"{actionName} took {stopwatch.Elapsed.TotalSeconds} Seconds");

			return result;
		}

		public static void ReportNeededTimeOnDebug(Action action, string actionName)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			action();
			stopwatch.Stop();

			Debug.WriteLine($"{actionName} took {stopwatch.Elapsed.TotalSeconds} Seconds");
		}

		public static async Task ReportNeededTimeOnDebug(Task action, string actionName)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			await action;
			stopwatch.Stop();

			Debug.WriteLine($"{actionName} took {stopwatch.Elapsed.TotalSeconds} Seconds");
		}

		public static async Task<T> ReportNeededTimeOnDebug<T>(Task<T> action, string actionName)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var result = await action;
			stopwatch.Stop();

			Debug.WriteLine($"{actionName} took {stopwatch.Elapsed.TotalSeconds} Seconds");

			return result;
		}
	}
}