using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Core;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.ViewModels.Controller
{
	public class StateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TDelta : IDelta<TModel>
		where TResult : ITestProcessingResult
	{
		private readonly IArtefactAdapter<TArtefact, TModel> artefactAdapter;
		private readonly IOfflineDeltaDiscoverer<TModel, TDelta> deltaDiscoverer;
		public ITestProcessor<TTestCase, TResult> TestProcessor { get; }
		private readonly ITestsDiscoverer<TModel, TTestCase> testsDiscoverer;
		private readonly ITestSelector<TModel, TDelta, TTestCase> testSelector;

		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;

		public StateBasedController(
			IArtefactAdapter<TArtefact, TModel> artefactAdapter,
			IOfflineDeltaDiscoverer<TModel, TDelta> deltaDiscoverer,
			ITestsDiscoverer<TModel, TTestCase> testsDiscoverer,
			ITestSelector<TModel, TDelta, TTestCase> testSelector,
			ITestProcessor<TTestCase, TResult> testProcessor)
		{
			this.artefactAdapter = artefactAdapter;
			this.deltaDiscoverer = deltaDiscoverer;
			TestProcessor = testProcessor;
			this.testsDiscoverer = testsDiscoverer;
			this.testSelector = testSelector;
		}

		public async Task<TResult> ExecuteImpactedTests(TArtefact oldArtefact, TArtefact newArtefact, CancellationToken token)
		{
			var oldModel = artefactAdapter.Parse(oldArtefact);
			var newModel = artefactAdapter.Parse(newArtefact);

			var delta = DebugStopWatchTracker.ReportNeededTimeOnDebug(() => deltaDiscoverer.Discover(oldModel, newModel), "DeltaDiscovery");
			token.ThrowIfCancellationRequested();

			var allTests = await DebugStopWatchTracker.ReportNeededTimeOnDebug(testsDiscoverer.GetTestCasesForModel(newModel, token), "TestsDiscovery");
			token.ThrowIfCancellationRequested();

			var impactedTests = new List<TTestCase>();
			testSelector.ImpactedTest += (sender, args) =>
			{
				var impactedTest = args.TestCase;
				ImpactedTest?.Invoke(sender, args);

				impactedTests.Add(impactedTest);
			};

			await DebugStopWatchTracker.ReportNeededTimeOnDebug(testSelector.SelectTests(allTests, delta, token), "Test Selector");

			Debug.WriteLine($"{impactedTests.Count} Tests impacted");

			token.ThrowIfCancellationRequested();

			var processingResult = await DebugStopWatchTracker.ReportNeededTimeOnDebug(TestProcessor.ProcessTests(impactedTests, token),
				"ProcessingOfImpactedTests");
			token.ThrowIfCancellationRequested();

			await DebugStopWatchTracker.ReportNeededTimeOnDebug(testSelector.UpdateInternalDataStructure(processingResult, token), "Internal DataStructure Update");

			return processingResult;
		}
	}
}