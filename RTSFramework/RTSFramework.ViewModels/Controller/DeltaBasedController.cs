using System;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.Contracts.Utilities;
using RTSFramework.RTSApproaches.Core;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.ViewModels.Controller
{
	public class DeltaBasedController<TDeltaArtefact, TModel, TDelta, TTestCase, TResult>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TDelta : IDelta<TModel>
		where TResult : ITestProcessingResult
	{
		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
		public event EventHandler<TestsPrioritizedEventArgs<TTestCase>> TestsPrioritized;

		private readonly IArtefactAdapter<TDeltaArtefact, TDelta> deltaArtefactAdapter;
		private readonly ITestsProcessor<TTestCase, TResult, TDelta, TModel> testsProcessor;
		private readonly ITestsDiscoverer<TModel, TTestCase> testsDiscoverer;
		private readonly ITestSelector<TModel, TDelta, TTestCase> testSelector;
		private readonly ITestsPrioritizer<TTestCase> testsPrioritizer;
		private readonly ILoggingHelper loggingHelper;

		public DeltaBasedController(
			IArtefactAdapter<TDeltaArtefact, TDelta> deltaArtefactAdapter,
			ITestsDiscoverer<TModel, TTestCase> testsDiscoverer,
			ITestSelector<TModel, TDelta, TTestCase> testSelector,
			ITestsProcessor<TTestCase, TResult, TDelta, TModel> testsProcessor,
			ITestsPrioritizer<TTestCase> testsPrioritizer,
			ILoggingHelper loggingHelper)
		{
			this.deltaArtefactAdapter = deltaArtefactAdapter;
			this.testsProcessor = testsProcessor;
			this.testsDiscoverer = testsDiscoverer;
			this.testSelector = testSelector;
			this.testsPrioritizer = testsPrioritizer;
			this.loggingHelper = loggingHelper;
		}

		public async Task<TResult> ExecuteImpactedTests(TDeltaArtefact deltaArtefact, CancellationToken token)
		{
			loggingHelper.InitLogFile();

			var delta = deltaArtefactAdapter.Parse(deltaArtefact);
			token.ThrowIfCancellationRequested();

			var allTests = await loggingHelper.ReportNeededTime(() => testsDiscoverer.GetTestCasesForModel(delta.NewModel, token), "Tests Discovery");
			token.ThrowIfCancellationRequested();

			var impactedTests = await loggingHelper.ReportNeededTime(() => testSelector.SelectTests(allTests, delta, token), "Tests Selection");

			foreach (var impactedTest in impactedTests)
			{
				ImpactedTest?.Invoke(this, new ImpactedTestEventArgs<TTestCase>(impactedTest));
			}

			loggingHelper.WriteMessage($"{impactedTests.Count} Tests impacted");

			var prioritizedTests = await loggingHelper.ReportNeededTime(() => testsPrioritizer.PrioritizeTests(impactedTests, token), "Tests Prioritization");

			TestsPrioritized?.Invoke(this, new TestsPrioritizedEventArgs<TTestCase>(prioritizedTests));

			var executor = testsProcessor as ITestsExecutor<TTestCase, TDelta, TModel>;
			if (executor != null)
			{
				executor.TestResultAvailable += TestResultAvailable;
			}
			var processingResult = await loggingHelper.ReportNeededTime(() => testsProcessor.ProcessTests(prioritizedTests, allTests, delta, token), "Tests Processing");
			if (executor != null)
			{
				executor.TestResultAvailable -= TestResultAvailable;
			}

			return processingResult;
		}
	}
}