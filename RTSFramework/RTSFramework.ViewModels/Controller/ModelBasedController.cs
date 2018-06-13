using System;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.Contracts.SecondaryFeature;
using RTSFramework.Contracts.Utilities;
using RTSFramework.RTSApproaches.Core;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.ViewModels.Controller
{
	public class ModelBasedController<TModel, TInputDelta, TSelectionDelta, TTestCase, TResult>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TInputDelta : IDelta<TModel>
		where TSelectionDelta : IDelta<TModel>
		where TResult : ITestProcessingResult
	{
		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
		public event EventHandler<TestsPrioritizedEventArgs<TTestCase>> TestsPrioritized;

		private readonly IDeltaAdapter<TInputDelta, TSelectionDelta, TModel> deltaAdapter;
		private readonly Lazy<IOfflineDeltaDiscoverer<TModel, TInputDelta>> deltaDiscoverer;
		private readonly ITestProcessor<TTestCase, TResult, TSelectionDelta, TModel> testProcessor;
		private readonly ITestDiscoverer<TModel, TSelectionDelta, TTestCase> testDiscoverer;
		private readonly ITestSelector<TModel, TSelectionDelta, TTestCase> testSelector;
		private readonly ITestPrioritizer<TTestCase> testPrioritizer;
		private readonly ILoggingHelper loggingHelper;
		private readonly Lazy<IDependenciesVisualizer> dependenciesVisualizer;

		public ModelBasedController(
			IDeltaAdapter<TInputDelta, TSelectionDelta, TModel> deltaAdapter,
			Lazy<IOfflineDeltaDiscoverer<TModel, TInputDelta>> deltaDiscoverer,
			ITestDiscoverer<TModel, TSelectionDelta , TTestCase> testDiscoverer,
			ITestSelector<TModel, TSelectionDelta, TTestCase> testSelector,
			ITestProcessor<TTestCase, TResult, TSelectionDelta, TModel> testProcessor,
			ITestPrioritizer<TTestCase> testPrioritizer,
			ILoggingHelper loggingHelper,
			Lazy<IDependenciesVisualizer> dependenciesVisualizer)
		{
			this.deltaAdapter = deltaAdapter;
			this.deltaDiscoverer = deltaDiscoverer;
			this.testProcessor = testProcessor;
			this.testDiscoverer = testDiscoverer;
			this.testSelector = testSelector;
			this.testPrioritizer = testPrioritizer;
			this.loggingHelper = loggingHelper;
			this.dependenciesVisualizer = dependenciesVisualizer;
		}

		public Func<TTestCase, bool> FilterFunction { private get; set; }

		public async Task<TResult> ExecuteRTSRun(TModel oldProgramModel, TModel newProgramModel, CancellationToken token)
		{
			var delta = deltaDiscoverer.Value.Discover(oldProgramModel, newProgramModel);
			token.ThrowIfCancellationRequested();

			return await ExecuteRTSRun(delta, token);
		}

		public virtual async Task<TResult> ExecuteRTSRun(TInputDelta delta, CancellationToken token)
		{
			var convertedDelta = deltaAdapter.Convert(delta);

			var allTests = await loggingHelper.ReportNeededTime(() => testDiscoverer.GetTests(convertedDelta, FilterFunction, token), "Tests Discovery");
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

			return processingResult;
		}

		public VisualizationData GetDependenciesVisualization()
		{
			return dependenciesVisualizer.Value.GetDependenciesVisualization(testSelector.CorrespondenceModel);
		}
	}
}