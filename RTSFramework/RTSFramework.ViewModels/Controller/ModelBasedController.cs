﻿using System;
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
	public class ModelBasedController<TProgram, TProgramDelta, TTestCase, TResult>
		where TTestCase : ITestCase
		where TProgram : IProgramModel
		where TProgramDelta : IDelta<TProgram>
		where TResult : ITestProcessingResult
	{
		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
		public event EventHandler<TestsPrioritizedEventArgs<TTestCase>> TestsPrioritized;

		private readonly Lazy<IOfflineDeltaDiscoverer<TProgram, TProgramDelta>> deltaDiscoverer;
		private readonly ITestProcessor<TTestCase, TResult, TProgramDelta, TProgram> testProcessor;
		private readonly ITestsDeltaAdapter<TProgram, TProgramDelta, TTestCase> testsDeltaAdapter;
		private readonly ITestSelector<TProgram, TProgramDelta, TTestCase> testSelector;
		private readonly ITestPrioritizer<TTestCase> testPrioritizer;
		private readonly ILoggingHelper loggingHelper;
		private readonly Lazy<IDependenciesVisualizer> dependenciesVisualizer;
		private readonly IResponsibleChangesReporter<TTestCase, TProgram, TProgramDelta> responsibleChangesReporter;

		public ModelBasedController(
			Lazy<IOfflineDeltaDiscoverer<TProgram, TProgramDelta>> deltaDiscoverer,
			ITestsDeltaAdapter<TProgram, TProgramDelta, TTestCase> testsDeltaAdapter,
			ITestSelector<TProgram, TProgramDelta, TTestCase> testSelector,
			ITestProcessor<TTestCase, TResult, TProgramDelta, TProgram> testProcessor,
			ITestPrioritizer<TTestCase> testPrioritizer,
			ILoggingHelper loggingHelper,
			Lazy<IDependenciesVisualizer> dependenciesVisualizer,
			IResponsibleChangesReporter<TTestCase, TProgram, TProgramDelta> responsibleChangesReporter)
		{
			this.deltaDiscoverer = deltaDiscoverer;
			this.testProcessor = testProcessor;
			this.testsDeltaAdapter = testsDeltaAdapter;
			this.testSelector = testSelector;
			this.testPrioritizer = testPrioritizer;
			this.loggingHelper = loggingHelper;
			this.dependenciesVisualizer = dependenciesVisualizer;
			this.responsibleChangesReporter = responsibleChangesReporter;
		}

		public Func<TTestCase, bool> FilterFunction { private get; set; }

		public async Task<TResult> ExecuteRTSRun(TProgram oldProgramModel, TProgram newProgramModel, CancellationToken token)
		{
			var programDelta = deltaDiscoverer.Value.Discover(oldProgramModel, newProgramModel);
			token.ThrowIfCancellationRequested();

			return await ExecuteRTSRun(programDelta, token);
		}

		public virtual async Task<TResult> ExecuteRTSRun(TProgramDelta programDelta, CancellationToken token)
		{
			var testsDelta = await loggingHelper.ReportNeededTime(() => testsDeltaAdapter.GetTestsDelta(programDelta, FilterFunction, token), "Tests Discovery");
			token.ThrowIfCancellationRequested();

			await loggingHelper.ReportNeededTime(() => testSelector.SelectTests(testsDelta, programDelta, token), "Tests Selection");
			var impactedTests = testSelector.SelectedTests;

			foreach (var impactedTest in impactedTests)
			{
				token.ThrowIfCancellationRequested();
				ImpactedTest?.Invoke(this, new ImpactedTestEventArgs<TTestCase>(impactedTest, 
					responsibleChangesReporter.GetResponsibleChanges(testSelector.CorrespondenceModel, impactedTest, programDelta)));
			}

			loggingHelper.WriteMessage($"{impactedTests.Count} Tests impacted");

			var prioritizedTests = await loggingHelper.ReportNeededTime(() => testPrioritizer.PrioritizeTests(impactedTests, token), "Tests Prioritization");

			TestsPrioritized?.Invoke(this, new TestsPrioritizedEventArgs<TTestCase>(prioritizedTests));

			var executor = testProcessor as ITestExecutor<TTestCase, TProgramDelta, TProgram>;
			if (executor != null)
			{
				executor.TestResultAvailable += TestResultAvailable;
			}
			var processingResult = await loggingHelper.ReportNeededTime(() => testProcessor.ProcessTests(prioritizedTests, testsDelta, programDelta, token), "Tests Processing");
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