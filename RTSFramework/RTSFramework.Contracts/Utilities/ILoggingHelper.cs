using System;
using System.Threading.Tasks;

namespace RTSFramework.Contracts.Utilities
{
	public interface ILoggingHelper
	{
		void InitLogFile();
		void WriteMessage(string message);
		T ReportNeededTime<T>(Func<T> action, string actionName);
		void ReportNeededTime(Action action, string actionName);
		Task ReportNeededTime(Func<Task> action, string actionName);
		Task<T> ReportNeededTime<T>(Func<Task<T>> action, string actionName);
	}
}