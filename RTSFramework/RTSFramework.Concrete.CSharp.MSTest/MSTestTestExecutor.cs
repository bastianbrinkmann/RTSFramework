using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using RTSFramework.Concrete.CSharp.MSTest.Adapters;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.MSTest.VsTest;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class MSTestTestExecutor<TProgramDelta, TProgram> : ITestExecutor<MSTestTestcase, TProgramDelta, TProgram>

		where TProgramDelta : IDelta<TProgram> where TProgram : IProgramModel
	{
		public event EventHandler<TestCaseResultEventArgs<MSTestTestcase>> TestResultAvailable;

		private readonly InProcessVsTestConnector vsTestConnector;
		private readonly ISettingsProvider settingsProvider;
		private readonly IArtefactAdapter<VsTestResultsToConvert, IList<ITestCaseResult<MSTestTestcase>>> testResultsAdapter;
		private readonly IArtefactAdapter<VsTestResultToConvert, ITestCaseResult<MSTestTestcase>> testResultAdapter;

		public MSTestTestExecutor(InProcessVsTestConnector vsTestConnector,
			ISettingsProvider settingsProvider,
			IArtefactAdapter<VsTestResultsToConvert, IList<ITestCaseResult<MSTestTestcase>>> testResultsAdapter,
			IArtefactAdapter<VsTestResultToConvert, ITestCaseResult<MSTestTestcase>> testResultAdapter)
		{
			this.vsTestConnector = vsTestConnector;
			this.settingsProvider = settingsProvider;
			this.testResultsAdapter = testResultsAdapter;
			this.testResultAdapter = testResultAdapter;
		}

		private IList<MSTestTestcase> msTestTestcases;

		public virtual async Task<ITestsExecutionResult<MSTestTestcase>> ProcessTests(IList<MSTestTestcase> impactedTests, StructuralDelta<TestsModel<MSTestTestcase>, MSTestTestcase> testsDelta, TProgramDelta programDelta, CancellationToken cancellationToken)
		{
			msTestTestcases = impactedTests;

			var vsTestResults = await ExecuteTests(msTestTestcases.Select(x => x.VsTestTestCase), cancellationToken);

			var result = new MSTestExectionResult();

			result.TestcasesResults.AddRange(testResultsAdapter.Parse(new VsTestResultsToConvert
			{
				MSTestTestcases = msTestTestcases,
				Results = vsTestResults
			}));

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
			TestResultAvailable?.Invoke(this, new TestCaseResultEventArgs<MSTestTestcase>(testResultAdapter.Parse(new VsTestResultToConvert
			{
				Result = vsTestResultEventArgs.VsTestResult,
				MSTestTestcase = msTestTestcases.SingleOrDefault(x => x.VsTestTestCase.Id == vsTestResultEventArgs.VsTestResult.TestCase.Id)
			})));
		}

	}
}