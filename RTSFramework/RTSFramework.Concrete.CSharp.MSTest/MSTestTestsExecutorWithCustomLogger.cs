using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Newtonsoft.Json;
using RTSFramework.Concrete.CSharp.MSTest.Adapters;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.MSTestLogger;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class MSTestTestsExecutorWithCustomLogger : MSTestTestsExecutor
	{
		public MSTestTestsExecutorWithCustomLogger(IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult> resultArtefactAdapter)
			: base(resultArtefactAdapter)
		{
		}

		public override void ProcessTests(IEnumerable<MSTestTestcase> tests)
		{
			//TODO Check wheter extension Dll exists and maybe whether version fits to framework version

			CurrentlyExecutedTests = tests as IList<MSTestTestcase> ?? tests.ToList();
			CurrentlyExecutedTests = CurrentlyExecutedTests.Where(x => !x.Ignored).ToList();
			if (CurrentlyExecutedTests.Any())
			{
				var arguments = BuildVsTestsArguments();

				using (AnonymousPipeServerStream pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
				{
					string clientHandle = pipeServer.GetClientHandleAsString();

					arguments += " /logger:rtsframework;PipeClientHandle=" + clientHandle;

					var executorProccess = new Process
					{
						StartInfo = new ProcessStartInfo
						{
							FileName = Path.Combine(MSTestConstants.VstestPath, MSTestConstants.Vstestconsole),
							Arguments = arguments,
							CreateNoWindow = true,
							UseShellExecute = false,
							RedirectStandardOutput = true
						}
					};

					executorProccess.Start();

					try
					{
						using (StreamReader sr = new StreamReader(pipeServer))
						{
							string line;
							do
							{
								line = sr.ReadLine();
								if (line != null && line != "RTSFrameworkTestRunComplete")
								{
									using (JsonTextReader jsonReader = new JsonTextReader(new StringReader(line)))
									{
										JsonSerializer serializer = JsonSerializer.Create();
										TestResultDto msTestResult = serializer.Deserialize<TestResultDto>(jsonReader);

										NotifyALlListeners(new MSTestTestResult
										{
											Outcome = msTestResult.Outcome == TestOutcome.Passed ? TestCaseResultType.Passed : TestCaseResultType.Failed,
											EndTime = msTestResult.EndTime,
											StartTime = msTestResult.StartTime,
											DurationInSeconds = msTestResult.Duration.Seconds,
											TestCaseId = msTestResult.DisplayName
										});
									}
								}
							} while (line != "RTSFrameworkTestRunComplete");
						}
					}
					catch (IOException e)
					{
						Console.WriteLine(e.Message);
					}
					pipeServer.DisposeLocalCopyOfClientHandle();
					executorProccess.WaitForExit();
				}

				ExecutionResults = ParseVsTestsTrxAnswer().TestcasesResults;
			}
		}

		private void NotifyALlListeners(MSTestTestResult result)
		{
			listeners.ForEach(x => x.NotifyTestResult(result));
		}

		public void RegisterListener(ITestResultListener<MSTestTestcase> listener)
		{
			listeners.Add(listener);
		}

		public void DeregisterListener(ITestResultListener<MSTestTestcase> listener)
		{
			listeners.Remove(listener);
		}

		private readonly List<ITestResultListener<MSTestTestcase>> listeners = new List<ITestResultListener<MSTestTestcase>>();
	}
}