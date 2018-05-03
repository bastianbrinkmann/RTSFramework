using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.Core.Utilities
{
	public class LoggingHelper : ILoggingHelper
	{
		private readonly ISettingsProvider settingsProvider;

		public LoggingHelper(ISettingsProvider settingsProvider)
		{
			this.settingsProvider = settingsProvider;
		}

		public void InitLogFile()
		{
			if (!settingsProvider.LogToFile)
			{
				return;
			}

			logFile = new FileInfo(Path.GetFullPath($"LogFiles\\logfile_{DateTime.Now:yy_MM_dd_hh_mm_ss}.txt"));

			if (logFile.DirectoryName != null && !Directory.Exists(logFile.DirectoryName))
			{
				Directory.CreateDirectory(logFile.DirectoryName);
			}
		}

		private FileInfo logFile;

		public void WriteMessage(string message)
		{
			Debug.WriteLine(message);

			if (settingsProvider.LogToFile)
			{
				using (var writer = logFile.AppendText())
				{
					writer.WriteLine(message);
				}
			}
		}

		private void WriteMessage(string actionName, double seconds)
		{
			var message = $"{actionName} took {seconds} Seconds";

			WriteMessage(message);
		}

		public T ReportNeededTime<T>(Func<T> action, string actionName)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			T result = action();
			stopwatch.Stop();

			WriteMessage(actionName, stopwatch.Elapsed.TotalSeconds);

			return result;
		}

		public void ReportNeededTime(Action action, string actionName)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			action();
			stopwatch.Stop();

			WriteMessage(actionName, stopwatch.Elapsed.TotalSeconds);
		}

		public async Task ReportNeededTime(Func<Task> action, string actionName)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			await action();
			stopwatch.Stop();

			WriteMessage(actionName, stopwatch.Elapsed.TotalSeconds);
		}

		public async Task<T> ReportNeededTime<T>(Func<Task<T>> action, string actionName)
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