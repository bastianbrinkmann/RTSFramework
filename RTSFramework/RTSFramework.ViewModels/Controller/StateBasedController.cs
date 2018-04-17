using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.RTSApproach;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.CorrespondenceModel;
using RTSFramework.RTSApproaches.Dynamic;

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
		private readonly ITestProcessor<TTestCase, TResult> testProcessor;
		private readonly Lazy<CorrespondenceModelManager> correspondenceModelManager;
		private readonly ITestsDiscoverer<TModel, TTestCase> testsDiscoverer;
		private readonly IRTSApproach<TModel, TDelta, TTestCase> rtsApproach;

		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;

		public StateBasedController(
			IArtefactAdapter<TArtefact, TModel> artefactAdapter,
			IOfflineDeltaDiscoverer<TModel, TDelta> deltaDiscoverer,
			ITestsDiscoverer<TModel, TTestCase> testsDiscoverer,
			IRTSApproach<TModel, TDelta, TTestCase> rtsApproach,
			ITestProcessor<TTestCase, TResult> testProcessor,
			Lazy<CorrespondenceModelManager> correspondenceModelManager)
		{
			this.artefactAdapter = artefactAdapter;
			this.deltaDiscoverer = deltaDiscoverer;
			this.testProcessor = testProcessor;
			this.correspondenceModelManager = correspondenceModelManager;
			this.testsDiscoverer = testsDiscoverer;
			this.rtsApproach = rtsApproach;
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
			rtsApproach.ImpactedTest += (sender, args) =>
			{
				var impactedTest = args.TestCase;
				ImpactedTest?.Invoke(sender, args);

				impactedTests.Add(impactedTest);
			};

			DebugStopWatchTracker.ReportNeededTimeOnDebug(() => rtsApproach.ExecuteRTS(allTests, delta, token), "RTSApproach");

			Debug.WriteLine($"{impactedTests.Count} Tests impacted");

			token.ThrowIfCancellationRequested();

			var processingResult = await DebugStopWatchTracker.ReportNeededTimeOnDebug(testProcessor.ProcessTests(impactedTests, token),
				"ProcessingOfImpactedTests");
			token.ThrowIfCancellationRequested();

			var resultWithCodeCoverage = processingResult as IExecutionWithCodeCoverageResult;
			if (resultWithCodeCoverage != null)
			{
				correspondenceModelManager.Value.CreateCorrespondenceModel(delta, allTests, resultWithCodeCoverage.CoverageData);
			}

			return processingResult;
		}
	}
}