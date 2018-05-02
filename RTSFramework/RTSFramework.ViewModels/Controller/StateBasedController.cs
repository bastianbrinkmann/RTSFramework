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
		public ITestProcessor<TTestCase, TResult, TDelta, TModel> TestProcessor { get; }
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;

		private readonly ITestsDiscoverer<TModel, TTestCase> testsDiscoverer;
		private readonly ITestSelector<TModel, TDelta, TTestCase> testSelector;

		public StateBasedController(
			IArtefactAdapter<TArtefact, TModel> artefactAdapter,
			IOfflineDeltaDiscoverer<TModel, TDelta> deltaDiscoverer,
			ITestsDiscoverer<TModel, TTestCase> testsDiscoverer,
			ITestSelector<TModel, TDelta, TTestCase> testSelector,
			ITestProcessor<TTestCase, TResult, TDelta, TModel> testProcessor)
		{
			this.artefactAdapter = artefactAdapter;
			this.deltaDiscoverer = deltaDiscoverer;
			TestProcessor = testProcessor;
			this.testsDiscoverer = testsDiscoverer;
			this.testSelector = testSelector;
		}

		public async Task<TResult> ExecuteImpactedTests(TArtefact oldArtefact, TArtefact newArtefact, CancellationToken token)
		{
			LoggingHelper.InitLogFile();

			var oldModel = artefactAdapter.Parse(oldArtefact);
			var newModel = artefactAdapter.Parse(newArtefact);

			var delta = LoggingHelper.ReportNeededTime(() => deltaDiscoverer.Discover(oldModel, newModel), "DeltaDiscovery");
			token.ThrowIfCancellationRequested();

			var allTests = await LoggingHelper.ReportNeededTime(() => testsDiscoverer.GetTestCasesForModel(newModel, token), "TestsDiscovery");
			token.ThrowIfCancellationRequested();

			var impactedTests = await LoggingHelper.ReportNeededTime(() => testSelector.SelectTests(allTests, delta, token), "Test Selector");

			foreach (var impactedTest in impactedTests)
			{
				ImpactedTest?.Invoke(this, new ImpactedTestEventArgs<TTestCase>(impactedTest));
			}

			LoggingHelper.WriteMessage($"{impactedTests.Count} Tests impacted");

			var processingResult = await LoggingHelper.ReportNeededTime(() => TestProcessor.ProcessTests(impactedTests, allTests, delta, token), "ProcessingOfImpactedTests");

			return processingResult;
		}
	}
}