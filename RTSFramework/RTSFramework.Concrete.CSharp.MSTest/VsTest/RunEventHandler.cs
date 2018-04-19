using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace RTSFramework.Concrete.CSharp.MSTest.VsTest
{
	public class RunEventHandler : ITestRunEventsHandler
	{
		private AsyncAutoResetEvent waitHandle;

		public List<TestResult> TestResults { get; } = new List<TestResult>();

		public ITestRunStatistics TestRunStatistics { get; private set; }

		public RunEventHandler(AsyncAutoResetEvent waitHandle)
		{
			this.waitHandle = waitHandle;
		}

		public void HandleTestRunComplete(
			TestRunCompleteEventArgs testRunCompleteArgs,
			TestRunChangedEventArgs lastChunkArgs,
			ICollection<AttachmentSet> runContextAttachments,
			ICollection<string> executorUris)
		{
			if (lastChunkArgs?.NewTestResults != null)
			{
				TestResults.AddRange(lastChunkArgs.NewTestResults);
			}
			if (testRunCompleteArgs != null)
			{
				TestRunStatistics = testRunCompleteArgs.TestRunStatistics;
			}
			
			waitHandle.Set();
		}

		public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
		{
			if (testRunChangedArgs?.NewTestResults != null)
			{
				TestResults.AddRange(testRunChangedArgs.NewTestResults);
			}
		}

		public void HandleLogMessage(TestMessageLevel level, string message)
		{

		}

		public void HandleRawMessage(string rawMessage)
		{

		}

		public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
		{
			return -1;
		}
	}
}