using System.Xml;
using System;
using System.IO;
using System.IO.Pipes;
using Microsoft.VisualStudio.TestTools.Execution;

namespace RTSFramework.Concrete.CSharp.MSTestDataCollector
{
	[DataCollectorTypeUri(DataCollectorTypeUri)]
	[DataCollectorFriendlyName(DataCollectorFriendlyName)]
	public class MSTestRTSFrameworkDataCollector : DataCollector
	{
		internal const string DataCollectorFriendlyName = "rtsframeworkcollector";
		internal const string DataCollectorTypeUri = "datacollector://" + DataCollectorFriendlyName + "/1.0";

		public const string TestRunComplete = "RTSFrameworkTestRunComplete";

		private PipeStream pipeClient;
		private StreamWriter writer;

		private DataCollectionEvents dataEvents;

		public override void Initialize(
			XmlElement configurationElement, 
			DataCollectionEvents events, 
			DataCollectionSink dataSink, 
			DataCollectionLogger logger,
			DataCollectionEnvironmentContext environmentContext)
		{
			string pipeClientHandle = configurationElement.FirstChild.InnerText;
			if (pipeClientHandle == null)
			{
				throw new Exception("PipeClientHandle must be specified!");
			}

			pipeClient = new AnonymousPipeClientStream(PipeDirection.Out, pipeClientHandle);
			writer = new StreamWriter(pipeClient) { AutoFlush = true };

			dataEvents = events;
			dataEvents.TestCaseStart += OnTestCaseExecutionStarted;
			dataEvents.TestCaseEnd += OnTestCaseExecutionFinished;
			dataEvents.TestStepStart += OnTestStepStart;
			dataEvents.TestStepEnd += OnTestStepEnd;
			dataEvents.SessionEnd += OnSessionEnd;
			dataEvents.SessionStart += OnSessionStart;
		}

		private void OnTestStepEnd(object sender, TestStepEndEventArgs testStepEndEventArgs)
		{
			File.AppendAllText(@"C:\TMP\Test.txt", "Step End: " + testStepEndEventArgs.TestStepDescription + ": " + testStepEndEventArgs.TestStepOutcome);
			writer.WriteLine("Step End: " + testStepEndEventArgs.TestStepDescription+ ": " + testStepEndEventArgs.TestStepOutcome);
		}

		private void OnTestStepStart(object sender, TestStepStartEventArgs testStepStartEventArgs)
		{
			File.AppendAllText(@"C:\TMP\Test.txt", "Step Start: " + testStepStartEventArgs.TestStepDescription);
			writer.WriteLine("Step Start: " + testStepStartEventArgs.TestStepDescription);
		}

		private void OnSessionStart(object sender, SessionStartEventArgs sessionStartEventArgs)
		{
			File.AppendAllText(@"C:\TMP\Test.txt", "Started Session");
			writer.WriteLine("Started Session");
		}

		private void OnSessionEnd(object sender, SessionEndEventArgs sessionEndEventArgs)
		{
			File.AppendAllText(@"C:\TMP\Test.txt", TestRunComplete);
			writer.WriteLine(TestRunComplete);
		}

		private void OnTestCaseExecutionFinished(object sender, TestCaseEndEventArgs testCaseEndEventArgs)
		{
			File.AppendAllText(@"C:\TMP\Test.txt", "Finished: " + testCaseEndEventArgs.TestCaseName + ": " + testCaseEndEventArgs.TestOutcome);
			writer.WriteLine("Finished: " + testCaseEndEventArgs.TestCaseName + ": " + testCaseEndEventArgs.TestOutcome);
		}

		private void OnTestCaseExecutionStarted(object sender, TestCaseStartEventArgs testCaseStartEventArgs)
		{
			File.AppendAllText(@"C:\TMP\Test.txt", "Started: " + testCaseStartEventArgs.TestCaseName);
			writer.WriteLine("Started: " + testCaseStartEventArgs.TestCaseName);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				File.AppendAllText(@"C:\TMP\Test.txt", "Disposing");

				dataEvents.TestCaseStart -= OnTestCaseExecutionStarted;
				dataEvents.TestCaseEnd -= OnTestCaseExecutionFinished;
				dataEvents.TestStepStart -= OnTestStepStart;
				dataEvents.TestStepEnd -= OnTestStepEnd;
				dataEvents.SessionEnd -= OnSessionEnd;
				dataEvents.SessionStart -= OnSessionStart;

				writer.Dispose();
				pipeClient.Dispose();
			}

			base.Dispose(disposing);
		}
	}
}