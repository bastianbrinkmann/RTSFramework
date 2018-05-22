using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.Contracts.Utilities;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Core;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.ViewModels.Controller
{
	public class StateBasedController<TArtefact, TModel, TDiscoveryDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact> : IController<TTestCase>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TDiscoveryDelta : IDelta<TModel>
		where TSelectionDelta : IDelta<TModel>
		where TResult : ITestProcessingResult
	{
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
		public event EventHandler<TestsPrioritizedEventArgs<TTestCase>> TestsPrioritized;

		private readonly IArtefactAdapter<TArtefact, TModel> artefactAdapter;
		private readonly IOfflineDeltaDiscoverer<TModel, TDiscoveryDelta> deltaDiscoverer;
		private readonly IDeltaAdapter<TDiscoveryDelta, TSelectionDelta, TModel> deltaAdapter;
		private readonly ITestsDiscoverer<TModel, TTestCase> testsDiscoverer;
		private readonly ITestsSelector<TModel, TSelectionDelta, TTestCase> testsSelector;
		private readonly ITestsProcessor<TTestCase, TResult, TSelectionDelta, TModel> testsProcessor;
		private readonly ITestsPrioritizer<TTestCase> testsPrioritizer;
		private readonly ILoggingHelper loggingHelper;
		private readonly IArtefactAdapter<TResultArtefact, TResult> resultArtefactAdapter;

		public StateBasedController(
			IArtefactAdapter<TArtefact, TModel> artefactAdapter,
			IOfflineDeltaDiscoverer<TModel, TDiscoveryDelta> deltaDiscoverer,
			IDeltaAdapter<TDiscoveryDelta, TSelectionDelta, TModel> deltaAdapter,
			ITestsDiscoverer<TModel, TTestCase> testsDiscoverer,
			ITestsSelector<TModel, TSelectionDelta, TTestCase> testsSelector,
			ITestsProcessor<TTestCase, TResult, TSelectionDelta, TModel> testsProcessor,
			ITestsPrioritizer<TTestCase> testsPrioritizer,
			ILoggingHelper loggingHelper,
			IArtefactAdapter<TResultArtefact, TResult> resultArtefactAdapter)
		{
			this.artefactAdapter = artefactAdapter;
			this.deltaDiscoverer = deltaDiscoverer;
			this.deltaAdapter = deltaAdapter;
			this.testsDiscoverer = testsDiscoverer;
			this.testsSelector = testsSelector;
			this.testsProcessor = testsProcessor;
			this.testsPrioritizer = testsPrioritizer;
			this.loggingHelper = loggingHelper;
			this.resultArtefactAdapter = resultArtefactAdapter;
		}

		public TArtefact OldArtefact { get; set; }
		public TArtefact NewArtefact { get; set; }
		public TResultArtefact Result { get; set; }

		public async Task ExecuteRTSRun(CancellationToken token)
		{
			loggingHelper.InitLogFile();

			var oldModel = artefactAdapter.Parse(OldArtefact);
			var newModel = artefactAdapter.Parse(NewArtefact);

			var discoveredDelta = loggingHelper.ReportNeededTime(() => deltaDiscoverer.Discover(oldModel, newModel), "Delta Discovery");
			token.ThrowIfCancellationRequested();

			var allTests = await loggingHelper.ReportNeededTime(() => testsDiscoverer.GetTestCasesForModel(newModel, token), "Tests Discovery");
			token.ThrowIfCancellationRequested();

			var convertedDelta = deltaAdapter.Convert(discoveredDelta);

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