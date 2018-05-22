using System;
using System.Collections.Generic;
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
	public class DeltaBasedController<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact> : IController<TTestCase>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TParsedDelta : IDelta<TModel>
		where TSelectionDelta : IDelta<TModel>
		where TResult : ITestProcessingResult
	{
		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
		public event EventHandler<TestsPrioritizedEventArgs<TTestCase>> TestsPrioritized;

		private readonly IArtefactAdapter<TDeltaArtefact, TParsedDelta> deltaArtefactAdapter;
		private readonly IDeltaAdapter<TParsedDelta, TSelectionDelta, TModel> deltaAdapter;
		private readonly ITestsProcessor<TTestCase, TResult, TSelectionDelta, TModel> testsProcessor;
		private readonly ITestsDiscoverer<TModel, TTestCase> testsDiscoverer;
		private readonly ITestsSelector<TModel, TSelectionDelta, TTestCase> testsSelector;
		private readonly ITestsPrioritizer<TTestCase> testsPrioritizer;
		private readonly ILoggingHelper loggingHelper;
		private readonly IArtefactAdapter<TResultArtefact, TResult> resultArtefactAdapter;

		public DeltaBasedController(
			IArtefactAdapter<TDeltaArtefact, TParsedDelta> deltaArtefactAdapter,
			IDeltaAdapter<TParsedDelta, TSelectionDelta, TModel> deltaAdapter,
			ITestsDiscoverer<TModel, TTestCase> testsDiscoverer,
			ITestsSelector<TModel, TSelectionDelta, TTestCase> testsSelector,
			ITestsProcessor<TTestCase, TResult, TSelectionDelta, TModel> testsProcessor,
			ITestsPrioritizer<TTestCase> testsPrioritizer,
			ILoggingHelper loggingHelper,
			IArtefactAdapter<TResultArtefact, TResult> resultArtefactAdapter)
		{
			this.deltaArtefactAdapter = deltaArtefactAdapter;
			this.deltaAdapter = deltaAdapter;
			this.testsProcessor = testsProcessor;
			this.testsDiscoverer = testsDiscoverer;
			this.testsSelector = testsSelector;
			this.testsPrioritizer = testsPrioritizer;
			this.loggingHelper = loggingHelper;
			this.resultArtefactAdapter = resultArtefactAdapter;
		}

		public TDeltaArtefact DeltaArtefact { get; set; }
		public TResultArtefact Result { get; set; }

		public async Task ExecuteRTSRun(CancellationToken token)
		{
			loggingHelper.InitLogFile();

			var parsedDelta = deltaArtefactAdapter.Parse(DeltaArtefact);
			token.ThrowIfCancellationRequested();

			var convertedDelta = deltaAdapter.Convert(parsedDelta);

			var allTests = await loggingHelper.ReportNeededTime(() => testsDiscoverer.GetTestCasesForModel(convertedDelta.NewModel, token), "Tests Discovery");
			token.ThrowIfCancellationRequested();

			var impactedTests = await loggingHelper.ReportNeededTime(() => testsSelector.SelectTests(allTests, convertedDelta, token), "Tests Selection");

			foreach (var impactedTest in impactedTests)
			{
				ImpactedTest?.Invoke(this, new ImpactedTestEventArgs<TTestCase>(impactedTest));
			}

			loggingHelper.WriteMessage($"{impactedTests.Count} Tests impacted");

			var prioritizedTests = await loggingHelper.ReportNeededTime(() => testsPrioritizer.PrioritizeTests(impactedTests, token), "Tests Prioritization");

			TestsPrioritized?.Invoke(this, new TestsPrioritizedEventArgs<TTestCase>(prioritizedTests));

			var executor = testsProcessor as ITestsExecutor<TTestCase, TSelectionDelta, TModel>;
			if (executor != null)
			{
				executor.TestResultAvailable += TestResultAvailable;
			}
			var processingResult = await loggingHelper.ReportNeededTime(() => testsProcessor.ProcessTests(prioritizedTests, allTests, convertedDelta, token), "Tests Processing");
			if (executor != null)
			{
				executor.TestResultAvailable -= TestResultAvailable;
			}

			Result = resultArtefactAdapter.Unparse(processingResult, Result);
		}
	}
}