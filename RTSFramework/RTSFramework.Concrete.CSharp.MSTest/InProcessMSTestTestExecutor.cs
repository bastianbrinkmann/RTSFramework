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
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class InProcessMSTestTestExecutor<TDelta, TModel> : ITestExecutor<MSTestTestcase, TDelta, TModel> where TDelta : IDelta<TModel> where TModel : IProgramModel
	{
		public event EventHandler<TestCaseResultEventArgs<MSTestTestcase>> TestResultAvailable;

		private readonly InProcessVsTestConnector vsTestConnector;
		private readonly ISettingsProvider settingsProvider;

		public InProcessMSTestTestExecutor(InProcessVsTestConnector vsTestConnector, ISettingsProvider settingsProvider)
		{
			this.vsTestConnector = vsTestConnector;
			this.settingsProvider = settingsProvider;
		}

		private IList<MSTestTestcase> msTestTestcases;

		public virtual async Task<ITestsExecutionResult<MSTestTestcase>> ProcessTests(IList<MSTestTestcase> impactedTests, IList<MSTestTestcase> allTests, TDelta impactedForDelta, CancellationToken cancellationToken)
		{
			msTestTestcases = impactedTests;

			var vsTestResults = await ExecuteTests(msTestTestcases.Select(x => x.VsTestTestCase), cancellationToken);

			var result = new MSTestExectionResult();
			result.TestcasesResults.AddRange(Convert(vsTestResults));

			if (settingsProvider.CleanupTestResultsDirectory)
			{
				var resultsPath = Path.GetFullPath(MSTestConstants.TestResultsFolder);
				DirectoryInfo resulsDirectory = new DirectoryInfo(resultsPath);

				foreach (DirectoryInfo dir in resulsDirectory.EnumerateDirectories("Deploy*"))
				{
					dir.Delete(true);
				}
			}

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
			var registration = token.Register(vsTestConnector.ConsoleWrapper.CancelTestRun);
			vsTestConnector.ConsoleWrapper.RunTests(testCases, string.Format(MSTestConstants.DefaultRunSettings, Directory.GetCurrentDirectory()), handler);

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