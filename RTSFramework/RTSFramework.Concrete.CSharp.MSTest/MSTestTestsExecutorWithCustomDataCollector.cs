using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Xml.Linq;
using RTSFramework.Concrete.CSharp.MSTest.Adapters;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.MSTestDataCollector;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class MSTestTestsExecutorWithCustomDataCollector : MSTestTestsExecutor
	{
		public MSTestTestsExecutorWithCustomDataCollector(IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult> resultArtefactAdapter)
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
					string settingsPath = Path.GetFullPath("Settings.runsettings");
					XDocument doc;
					using (FileStream stream = File.OpenRead(settingsPath))
					{
						doc = XDocument.Load(stream);
					}
					var clientHandleNode = doc.Descendants("PipeClientHandle").Single();
					clientHandleNode.Value = clientHandle;

					using (FileStream stream = File.OpenWrite(settingsPath))
					{
						doc.Save(stream);
					}

					arguments += " /Settings:" + settingsPath;

					var executorProccess = new Process
					{
						StartInfo = new ProcessStartInfo
						{
							FileName = Path.Combine(MSTestConstants.VstestPath, MSTestConstants.Vstestconsole),
							Arguments = arguments,
							CreateNoWindow = true,
							UseShellExecute = false,
							RedirectStandardOutput = false
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
								if (line != null && line != MSTestRTSFrameworkDataCollector.TestRunComplete)
								{
									Console.WriteLine(line);
								}
							} while (line != MSTestRTSFrameworkDataCollector.TestRunComplete);
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