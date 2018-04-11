using System;
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
        private readonly Func<DiscoveryType, IOfflineDeltaDiscoverer<TModel, TDelta>> deltaDiscovererFactory;
        private readonly Func<ProcessingType, ITestProcessor<TTestCase, TResult>> testProcessorFactory;
        private readonly ITestsDiscoverer<TModel, TTestCase> testsDiscoverer;
        private readonly Func<RTSApproachType, IRTSApproach<TDelta,TTestCase>> rtsApproachFactory;

        public StateBasedController(
			Func<DiscoveryType, IOfflineDeltaDiscoverer<TModel, TDelta>> deltaDiscovererFactory,
            ITestsDiscoverer<TModel, TTestCase> testsDiscoverer,
            Func<RTSApproachType, IRTSApproach<TDelta, TTestCase>> rtsApproachFactory,
			Func<ProcessingType, ITestProcessor<TTestCase, TResult>> testProcessorFactory)
        {
            this.deltaDiscovererFactory = deltaDiscovererFactory;
            this.testProcessorFactory = testProcessorFactory;
            this.testsDiscoverer = testsDiscoverer;
            this.rtsApproachFactory = rtsApproachFactory;
        }

        private TDelta PerformDeltaDiscovery(RunConfiguration<TModel> configuration)
        {
            var deltaDiscoverer = deltaDiscovererFactory(configuration.DiscoveryType);

	        var delta = DebugStopWatchTracker.ReportNeededTimeOnDebug(() => deltaDiscoverer.Discover(configuration.OldProgramModel, configuration.NewProgramModel), "DeltaDiscovery");

            return delta;
        }
		public async Task<TResult> ExecuteImpactedTests(RunConfiguration<TModel> configuration, CancellationToken token)
		{
			var testProcessor = testProcessorFactory(configuration.ProcessingType);
			var rtsApproach = rtsApproachFactory(configuration.RTSApproachType);

			var delta = PerformDeltaDiscovery(configuration);
			if (token.IsCancellationRequested)
			{
				return default(TResult);
			}

			var allTests = await DebugStopWatchTracker.ReportNeededTimeOnDebug(testsDiscoverer.GetTestCasesForModel(configuration.NewProgramModel, token), "TestsDiscovery");
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