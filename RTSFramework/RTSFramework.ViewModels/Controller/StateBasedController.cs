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
		private readonly ITestDiscoverer<TModel, TTestCase> testDiscoverer;
		private readonly ITestSelector<TModel, TSelectionDelta, TTestCase> testSelector;
		private readonly ITestProcessor<TTestCase, TResult, TSelectionDelta, TModel> testProcessor;
		private readonly ITestPrioritizer<TTestCase> testPrioritizer;
		private readonly ILoggingHelper loggingHelper;
		private readonly IArtefactAdapter<TResultArtefact, TResult> resultArtefactAdapter;

		public StateBasedController(
			IArtefactAdapter<TArtefact, TModel> artefactAdapter,
			IOfflineDeltaDiscoverer<TModel, TDiscoveryDelta> deltaDiscoverer,
			IDeltaAdapter<TDiscoveryDelta, TSelectionDelta, TModel> deltaAdapter,
			ITestDiscoverer<TModel, TTestCase> testDiscoverer,
			ITestSelector<TModel, TSelectionDelta, TTestCase> testSelector,
			ITestProcessor<TTestCase, TResult, TSelectionDelta, TModel> testProcessor,
			ITestPrioritizer<TTestCase> testPrioritizer,
			ILoggingHelper loggingHelper,
			IArtefactAdapter<TResultArtefact, TResult> resultArtefactAdapter)
		{
			this.artefactAdapter = artefactAdapter;
			this.deltaDiscoverer = deltaDiscoverer;
			this.deltaAdapter = deltaAdapter;
			this.testDiscoverer = testDiscoverer;
			this.testSelector = testSelector;
			this.testProcessor = testProcessor;
			this.testPrioritizer = testPrioritizer;
			this.loggingHelper = loggingHelper;
			this.resultArtefactAdapter = resultArtefactAdapter;
		}

		public Func<TTestCase, bool> FilterFunction { private get; set; }
		public TArtefact OldArtefact { private get; set; }
		public TArtefact NewArtefact { private get; set; }
		public TResultArtefact Result { get; private set; }

		public async Task ExecuteRTSRun(CancellationToken token)
		{
			loggingHelper.InitLogFile();

			var oldModel = artefactAdapter.Parse(OldArtefact);
			var newModel = artefactAdapter.Parse(NewArtefact);

			var discoveredDelta = loggingHelper.ReportNeededTime(() => deltaDiscoverer.Discover(oldModel, newModel), "Delta Discovery");
			token.ThrowIfCancellationRequested();

			var convertedDelta = deltaAdapter.Convert(discoveredDelta);

			var allTests = await loggingHelper.ReportNeededTime(() => testDiscoverer.GetTests(newModel, FilterFunction, token), "Tests Discovery");
			token.ThrowIfCancellationRequested();

			await loggingHelper.ReportNeededTime(() => testSelector.SelectTests(allTests, convertedDelta, token), "Tests Selection");
			var impactedTests = testSelector.SelectedTests;

			foreach (var impactedTest in impactedTests)
			{
				ImpactedTest?.Invoke(this, new ImpactedTestEventArgs<TTestCase>(impactedTest, testSelector.GetResponsibleChangesByTestId?.Invoke(impactedTest.Id)));
			}

			loggingHelper.WriteMessage($"{impactedTests.Count} Tests impacted");

			var prioritizedTests = await loggingHelper.ReportNeededTime(() => testPrioritizer.PrioritizeTests(impactedTests, token), "Tests Prioritization");

			TestsPrioritized?.Invoke(this, new TestsPrioritizedEventArgs<TTestCase>(prioritizedTests));

			var executor = testProcessor as ITestExecutor<TTestCase, TSelectionDelta, TModel>;
			if (executor != null)
			{
				executor.TestResultAvailable += TestResultAvailable;
			}
			var processingResult = await loggingHelper.ReportNeededTime(() => testProcessor.ProcessTests(prioritizedTests, allTests, convertedDelta, token), "Tests Processing");
			if (executor != null)
			{
				executor.TestResultAvailable -= TestResultAvailable;
			}

			Result = resultArtefactAdapter.Unparse(processingResult, Result);
		}
	}
}