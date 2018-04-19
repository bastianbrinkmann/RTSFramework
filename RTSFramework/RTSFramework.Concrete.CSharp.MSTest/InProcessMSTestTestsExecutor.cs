using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.MSTest.VsTest;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class InProcessMSTestTestsExecutor : ITestProcessor<MSTestTestcase, MSTestExectionResult>
	{
		private readonly InProcessVsTestConnector vsTestConnector;

		public InProcessMSTestTestsExecutor(InProcessVsTestConnector vsTestConnector)
		{
			this.vsTestConnector = vsTestConnector;
		}

		public virtual async Task<MSTestExectionResult> ProcessTests(IEnumerable<MSTestTestcase> tests, CancellationToken token)
		{
			var msTestTestcases = tests as IList<MSTestTestcase> ?? tests.ToList();

			var vsTestResults = await ExecuteTests(msTestTestcases.Select(x => x.VsTestTestCase), token);

			var result = new MSTestExectionResult();
			result.TestcasesResults.AddRange(Convert(vsTestResults, msTestTestcases));
			
			return result;
		}

		private IList<ITestCaseResult<MSTestTestcase>> Convert(IList<TestResult> vsTestResults, IList<MSTestTestcase> msTestTestcases)
		{
			var msTestResults = new List<ITestCaseResult<MSTestTestcase>>();

			foreach (var vsTestResult in vsTestResults)
			{
				TestExecutionOutcome outcome;
				switch (vsTestResult.Outcome)
				{
					case Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed:
						outcome = TestExecutionOutcome.Passed;
						break;
					case Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed:
						outcome = TestExecutionOutcome.Failed;
						break;
					default:
						outcome = TestExecutionOutcome.NotExecuted;
						break;
				}

				var msTestTestcase = msTestTestcases.Single(x => x.VsTestTestCase.Id == vsTestResult.TestCase.Id);

				var dataDrivenProperty = vsTestResult.TestCase.Properties.SingleOrDefault(x => x.Id == "MSTestDiscoverer.IsDataDriven");
				bool isDataDriven = dataDrivenProperty != null && vsTestResult.TestCase.GetPropertyValue(dataDrivenProperty, false);

				if (isDataDriven)
				{
					var childResult = new MSTestTestResult
					{
						TestCase = msTestTestcase,
						Outcome = outcome,
						StartTime = vsTestResult.StartTime,
						EndTime = vsTestResult.EndTime,
						ErrorMessage = vsTestResult.ErrorMessage,
						StackTrace = vsTestResult.ErrorStackTrace,
						DurationInSeconds = vsTestResult.Duration.TotalSeconds
					};

					if (msTestResults.Any(x => x.TestCase.Id == msTestTestcase.Id))
					{
						var compositeTestCase = (CompositeTestCaseResult<MSTestTestcase>) msTestResults.Single(x => x.TestCase.Id == msTestTestcase.Id);
						compositeTestCase.ChildrenResults.Add(childResult);
					}
					else
					{
						var compositeTestCase = new CompositeTestCaseResult<MSTestTestcase>
						{
							TestCase = msTestTestcase
						};
						compositeTestCase.ChildrenResults.Add(childResult);
						msTestResults.Add(compositeTestCase);
					}
				}
				else
				{
					msTestResults.Add(new MSTestTestResult
					{
						TestCase = msTestTestcase,
						Outcome = outcome,
						StartTime = vsTestResult.StartTime,
						EndTime = vsTestResult.EndTime,
						ErrorMessage = vsTestResult.ErrorMessage,
						StackTrace = vsTestResult.ErrorStackTrace,
						DurationInSeconds = vsTestResult.Duration.TotalSeconds
					});
				}
			}

			return msTestResults;
		}

		private async Task<IList<TestResult>> ExecuteTests(IEnumerable<TestCase> testCases, CancellationToken token)
		{
			var waitHandle = new AsyncAutoResetEvent();
			var handler = new RunEventHandler(waitHandle);

			vsTestConnector.ConsoleWrapper.RunTests(testCases, MSTestConstants.DefaultRunSettings, handler);
			var registration = token.Register(vsTestConnector.ConsoleWrapper.CancelTestRun);

			await waitHandle.WaitAsync(token);
			registration.Dispose();
			token.ThrowIfCancellationRequested();

			return handler.TestResults;
		}
	}
}