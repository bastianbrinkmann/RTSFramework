﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.RTSApproach;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Dynamic;
using RTSFramework.ViewModels.RunConfigurations;

namespace RTSFramework.ViewModels
{
    public class StateBasedController<TModel, TDelta, TTestCase, TResult> 
        where TTestCase : ITestCase 
		where TModel : IProgramModel 
		where TDelta : IDelta
		where TResult : ITestProcessingResult
    {
        private readonly IOfflineDeltaDiscoverer<TModel, TDelta> deltaDiscoverer;
        private readonly ITestProcessor<TTestCase, TResult> testProcessor;
        private readonly ITestsDiscoverer<TModel, TTestCase> testsDiscoverer;
        private readonly IRTSApproach<TDelta,TTestCase> rtsApproach;

        public StateBasedController(
			IOfflineDeltaDiscoverer<TModel, TDelta> deltaDiscoverer,
            ITestsDiscoverer<TModel, TTestCase> testsDiscoverer,
			IRTSApproach<TDelta, TTestCase> rtsApproach,
			ITestProcessor<TTestCase, TResult> testProcessor)
        {
            this.deltaDiscoverer = deltaDiscoverer;
            this.testProcessor = testProcessor;
            this.testsDiscoverer = testsDiscoverer;
            this.rtsApproach = rtsApproach;
        }

		public async Task<TResult> ExecuteImpactedTests(TModel oldModel, TModel newModel, CancellationToken token)
		{
			var delta = DebugStopWatchTracker.ReportNeededTimeOnDebug(() => deltaDiscoverer.Discover(oldModel, newModel), "DeltaDiscovery");
			if (token.IsCancellationRequested)
			{
				return default(TResult);
			}

			var allTests = await DebugStopWatchTracker.ReportNeededTimeOnDebug(testsDiscoverer.GetTestCasesForModel(newModel, token), "TestsDiscovery");
			if (token.IsCancellationRequested)
			{
				return default(TResult);
			}

			var impactedTests = new List<TTestCase>();
			rtsApproach.ImpactedTest += (sender, args) =>
			{
				var impactedTest = args.TestCase;

				Debug.WriteLine($"Impacted Test: {impactedTest.Id}");
				impactedTests.Add(impactedTest);
			};

			DebugStopWatchTracker.ReportNeededTimeOnDebug(() => rtsApproach.ExecuteRTS(allTests, delta, token), "RTSApproach");

			Debug.WriteLine($"{impactedTests.Count} Tests impacted");

			if (token.IsCancellationRequested)
			{
				return default(TResult);
			}

			var processingResult = await DebugStopWatchTracker.ReportNeededTimeOnDebug(testProcessor.ProcessTests(impactedTests, token), "ProcessingOfImpactedTests");
			if (token.IsCancellationRequested)
			{
				return default(TResult);
			}

			var testExecutionResult = processingResult as MSTestExectionResult;
			if (testExecutionResult != null && testExecutionResult.CoverageData != null)
			{
				var dynamicRtsApproach = rtsApproach as IDynamicRTSApproach;
				dynamicRtsApproach?.UpdateCorrespondenceModel(testExecutionResult.CoverageData);
			}

			return processingResult;
		}
    }
}