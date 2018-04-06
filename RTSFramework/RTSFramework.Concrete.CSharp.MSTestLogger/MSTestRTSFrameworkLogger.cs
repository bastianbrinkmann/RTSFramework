using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Newtonsoft.Json;

namespace RTSFramework.Concrete.CSharp.MSTestLogger
{
	[ExtensionUri(ExtensionId)]
	[FriendlyName(FriendlyName)]
	public class MSTestRTSFrameworkLogger : ITestLoggerWithParameters
	{
		internal const string ExtensionId = "logger://" + FriendlyName;
		private const string FriendlyName = "rtsframework";

		public const string TestRunComplete = "RTSFrameworkTestRunComplete";

		private PipeStream pipeClient;
		private StreamWriter writer;
		
		public void Initialize(TestLoggerEvents events, Dictionary<string, string> parameters)
		{
			string pipeClientHandle = parameters["PipeClientHandle"];
			pipeClient = new AnonymousPipeClientStream(PipeDirection.Out, pipeClientHandle);
			writer = new StreamWriter(pipeClient) {AutoFlush = true};

			events.TestRunMessage += OnTestRunMessage;
			events.TestResult += OnTestResult;
			events.TestRunComplete += OnTestRunComplete;
		}

		public void Initialize(TestLoggerEvents events, string testRunDirectory)
		{
			throw new InvalidOperationException();
		}

		private void OnTestRunComplete(object sender, TestRunCompleteEventArgs e)
		{
			writer.WriteLine(TestRunComplete);
			writer.Dispose();
			pipeClient.Dispose();
		}

		private void OnTestResult(object sender, TestResultEventArgs e)
		{
			var serializer = JsonSerializer.Create();
			var stringWriter = new StringWriter();

			var dto = new TestResultDto
			{
				DisplayName = e.Result.DisplayName,
				Duration = e.Result.Duration,
				Outcome = e.Result.Outcome,
				EndTime = e.Result.EndTime,
				StartTime = e.Result.StartTime
			};

			serializer.Serialize(stringWriter, dto);

			writer.WriteLine(stringWriter.ToString());
		}

		private void OnTestRunMessage(object sender, TestRunMessageEventArgs e)
		{
			if (e.Message != null)
			{
				writer.WriteLine(e.Message);
			}
		}

		
	}
}