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
	public class StateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TDelta : IDelta<TModel>
		where TResult : ITestProcessingResult
	{
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
		public event EventHandler<TestsPrioritizedEventArgs<TTestCase>> TestsPrioritized;

		private readonly IArtefactAdapter<TArtefact, TModel> artefactAdapter;
		private readonly IOfflineDeltaDiscoverer<TModel, TDelta> deltaDiscoverer;
		private readonly ITestsDiscoverer<TModel, TTestCase> testsDiscoverer;
		private readonly ITestSelector<TModel, TDelta, TTestCase> testSelector;
		private readonly ITestsProcessor<TTestCase, TResult, TDelta, TModel> testsProcessor;
		private readonly ITestsPrioritizer<TTestCase> testsPrioritizer;
		private readonly ILoggingHelper loggingHelper;

		public StateBasedController(
			IArtefactAdapter<TArtefact, TModel> artefactAdapter,
			IOfflineDeltaDiscoverer<TModel, TDelta> deltaDiscoverer,
			ITestsDiscoverer<TModel, TTestCase> testsDiscoverer,
			ITestSelector<TModel, TDelta, TTestCase> testSelector,
			ITestsProcessor<TTestCase, TResult, TDelta, TModel> testsProcessor,
			ITestsPrioritizer<TTestCase> testsPrioritizer,
			ILoggingHelper loggingHelper)
		{
			this.artefactAdapter = artefactAdapter;
			this.deltaDiscoverer = deltaDiscoverer;
			this.testsDiscoverer = testsDiscoverer;
			this.testSelector = testSelector;
			this.testsProcessor = testsProcessor;
			this.testsPrioritizer = testsPrioritizer;
			this.loggingHelper = loggingHelper;
		}

		public async Task<TResult> ExecuteImpactedTests(TArtefact oldArtefact, TArtefact newArtefact, CancellationToken token)
		{
			loggingHelper.InitLogFile();

			var oldModel = artefactAdapter.Parse(oldArtefact);
			var newModel = artefactAdapter.Parse(newArtefact);

			var delta = loggingHelper.ReportNeededTime(() => deltaDiscoverer.Discover(oldModel, newModel), "Delta Discovery");
			token.ThrowIfCancellationRequested();

			var allTests = await loggingHelper.ReportNeededTime(() => testsDiscoverer.GetTestCasesForModel(newModel, token), "Tests Discovery");
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