using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace RTSFramework.Concrete.CSharp.MSTest.VsTest
{
	public class DiscoveryEventHandler : ITestDiscoveryEventsHandler
	{
		private readonly AsyncAutoResetEvent waitHandle;

		public List<TestCase> DiscoveredTestCases { get; }

		public DiscoveryEventHandler(AsyncAutoResetEvent waitHandle)
		{
			this.waitHandle = waitHandle;
			DiscoveredTestCases = new List<TestCase>();
		}

		public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
		{
			if (discoveredTestCases != null)
			{
				DiscoveredTestCases.AddRange(discoveredTestCases);
			}
		}

		public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted)
		{
			if (lastChunk != null)
			{
				DiscoveredTestCases.AddRange(lastChunk);
			}

			waitHandle.Set();
		}

		public void HandleLogMessage(TestMessageLevel level, string message)
		{
		}

		public void HandleRawMessage(string rawMessage)
		{
		}
	}
}