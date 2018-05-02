using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.MSTest.VsTest;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class InProcessMSTestTestsExecutor<TDelta, TModel> : ITestExecutor<MSTestTestcase, TDelta, TModel> where TDelta : IDelta<TModel> where TModel : IProgramModel
	{
		public event EventHandler<TestCaseResultEventArgs<MSTestTestcase>> TestResultAvailable;

		private readonly InProcessVsTestConnector vsTestConnector;

		public InProcessMSTestTestsExecutor(InProcessVsTestConnector vsTestConnector)
		{
			this.vsTestConnector = vsTestConnector;
		}

		private IList<MSTestTestcase> msTestTestcases;

		public virtual async Task<ITestExecutionResult<MSTestTestcase>> ProcessTests(IList<MSTestTestcase> impactedTests, IList<MSTestTestcase> allTests, TDelta impactedForDelta, CancellationToken cancellationToken)
		{
			msTestTestcases = impactedTests;

			var vsTestResults = await ExecuteTests(msTestTestcases.Select(x => x.VsTestTestCase), cancellationToken);

			var result = new MSTestExectionResult();
			result.TestcasesResults.AddRange(Convert(vsTestResults));
			
			return result;
		}

		private IList<ITestCaseResult<MSTestTestcase>> Convert(IList<TestResult> vsTestResults)
		{
			var msTestResults = new List<ITestCaseResult<MSTestTestcase>>();

			foreach (var vsTestResult in vsTestResults)
			{
				var singleResult = Convert(vsTestResult);

				if (singleResult.TestCase.IsChildTestCase)
				{
					if (msTestResults.Any(x => x.TestCase.Id == singleResult.TestCase.Id))
					{
						var compositeTestCase = (CompositeTestCaseResult<MSTestTestcase>) msTestResults.Single(x => x.TestCase.Id == singleResult.TestCase.Id);
						compositeTestCase.ChildrenResults.Add(singleResult);
					}
					else
					{
						var compositeTestCase = new CompositeTestCaseResult<MSTestTestcase>
						{
							TestCase = singleResult.TestCase
						};
						compositeTestCase.ChildrenResults.Add(singleResult);
						msTestResults.Add(compositeTestCase);
					}
				}
				else
				{
					msTestResults.Add(singleResult);
				}
			}

			return msTestResults;
		}

		private ITestCaseResult<MSTestTestcase> Convert(TestResult vsTestResult)
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

			return new MSTestTestResult
			{
				TestCase = msTestTestcase,
				Outcome = outcome,
				StartTime = vsTestResult.StartTime,
				EndTime = vsTestResult.EndTime,
				ErrorMessage = vsTestResult.ErrorMessage,
				StackTrace = vsTestResult.ErrorStackTrace,
				DurationInSeconds = vsTestResult.Duration.TotalSeconds,
				DisplayName = vsTestResult.DisplayName
			};
		}

		private async Task<IList<TestResult>> ExecuteTests(IEnumerable<TestCase> testCases, CancellationToken token)
		{
			var waitHandle = new AsyncAutoResetEvent();
			var handler = new RunEventHandler(waitHandle);
			handler.TestResultAvailable += HandlerOnTestResultAvailable;
			vsTestConnector.ConsoleWrapper.RunTests(testCases, string.Format(MSTestConstants.DefaultRunSettings, Directory.GetCurrentDirectory()), handler);
			var registration = token.Register(vsTestConnector.ConsoleWrapper.CancelTestRun);

			await waitHandle.WaitAsync(token);
			handler.TestResultAvailable -= HandlerOnTestResultAvailable;
			registration.Dispose();

			token.ThrowIfCancellationRequested();

			return handler.TestResults;
		}

		private void HandlerOnTestResultAvailable(object sender, VsTestResultEventArgs vsTestResultEventArgs)
		{
			TestResultAvailable?.Invoke(this, new TestCaseResultEventArgs<MSTestTestcase>(Convert(vsTestResultEventArgs.VsTestResult)));
		}
	}
}