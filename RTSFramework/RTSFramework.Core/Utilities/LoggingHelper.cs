using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace RTSFramework.Core.Utilities
{
	public static class LoggingHelper
	{
		public static void InitLogFile()
		{
			logFile = new FileInfo(Path.GetFullPath($"LogFiles\\logfile_{DateTime.Now:yy_MM_dd_hh_mm_ss}.txt"));

			if (logFile.DirectoryName != null && !Directory.Exists(logFile.DirectoryName))
			{
				Directory.CreateDirectory(logFile.DirectoryName);
			}
		}

		private static FileInfo logFile;

		public static void WriteMessage(string message)
		{
			Debug.WriteLine(message);

			using (var writer = logFile.AppendText())
			{
				writer.WriteLine(message);
			}
		}

		private static void WriteMessage(string actionName, double seconds)
		{
			var message = $"{actionName} took {seconds} Seconds";

			WriteMessage(message);
		}

		public static T ReportNeededTime<T>(Func<T> action, string actionName)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			T result = action();
			stopwatch.Stop();

			WriteMessage(actionName, stopwatch.Elapsed.TotalSeconds);

			return result;
		}

		public static void ReportNeededTime(Action action, string actionName)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			action();
			stopwatch.Stop();

			WriteMessage(actionName, stopwatch.Elapsed.TotalSeconds);
		}

		public static async Task ReportNeededTime(Func<Task> action, string actionName)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			await action();
			stopwatch.Stop();

			WriteMessage(actionName, stopwatch.Elapsed.TotalSeconds);
		}

		public static async Task<T> ReportNeededTime<T>(Func<Task<T>> action, string actionName)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			T result = await action();
			stopwatch.Stop();

			WriteMessage(actionName, stopwatch.Elapsed.TotalSeconds);

			return result;
		}
	}
}