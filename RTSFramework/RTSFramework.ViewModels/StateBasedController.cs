using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.RTSApproach;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Dynamic;
using RTSFramework.ViewModels.RunConfigurations;

namespace RTSFramework.ViewModels
{
    public class StateBasedController<TModel, TDelta, TTestCase> 
        where TTestCase : ITestCase 
		where TModel : IProgramModel 
		where TDelta : IDelta
    {
        private readonly Func<DiscoveryType, IOfflineDeltaDiscoverer<TModel, TDelta>> deltaDiscovererFactory;
        private readonly Func<ProcessingType, ITestProcessor<TTestCase>> testProcessorFactory;
        private readonly ITestsDiscoverer<TModel, TTestCase> testsDiscoverer;
        private readonly Func<RTSApproachType, IRTSApproach<TDelta,TTestCase>> rtsApproachFactory;

        public StateBasedController(
			Func<DiscoveryType, IOfflineDeltaDiscoverer<TModel, TDelta>> deltaDiscovererFactory,
            ITestsDiscoverer<TModel, TTestCase> testsDiscoverer,
            Func<RTSApproachType, IRTSApproach<TDelta, TTestCase>> rtsApproachFactory,
			Func<ProcessingType, ITestProcessor<TTestCase>> testProcessorFactory)
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

	    private const string Cancelled = "Cancelled";
		public async Task<string> ExecuteImpactedTests(RunConfiguration<TModel> configuration, CancellationToken token)
		{
			StringBuilder resultBuilder = new StringBuilder();

			var testProcessor = testProcessorFactory(configuration.ProcessingType);
			var rtsApproach = rtsApproachFactory(configuration.RTSApproachType);

			var delta = PerformDeltaDiscovery(configuration);
			if (token.IsCancellationRequested)
			{
				resultBuilder.AppendLine(Cancelled);
				return resultBuilder.ToString();
			}

			var allTests = await DebugStopWatchTracker.ReportNeededTimeOnDebug(testsDiscoverer.GetTestCasesForModel(configuration.NewProgramModel, token), "TestsDiscovery");
			if (token.IsCancellationRequested)
			{
				resultBuilder.AppendLine(Cancelled);
				return resultBuilder.ToString();
			}

			ImpactedTests = new List<TTestCase>();
			rtsApproach.ImpactedTest += NotifyImpactedTest;
			DebugStopWatchTracker.ReportNeededTimeOnDebug(() => rtsApproach.ExecuteRTS(allTests, delta, token), "RTSApproach");
			rtsApproach.ImpactedTest -= NotifyImpactedTest;

			resultBuilder.AppendLine($"{ImpactedTests.Count} Tests impacted");
			if (token.IsCancellationRequested)
			{
				resultBuilder.AppendLine(Cancelled);
				return resultBuilder.ToString();
			}

			await DebugStopWatchTracker.ReportNeededTimeOnDebug(testProcessor.ProcessTests(ImpactedTests, token), "ProcessingOfImpactedTests");
			if (token.IsCancellationRequested)
			{
				resultBuilder.AppendLine(Cancelled);
				return resultBuilder.ToString();
			}

			var processorWithCoverageCollection = testProcessor as IAutomatedTestsExecutorWithCoverageCollection<TTestCase>;
			var coverageResults = processorWithCoverageCollection?.GetCollectedCoverageData();
			if (coverageResults != null)
			{
				var dynamicRtsApproach = rtsApproach as IDynamicRTSApproach;
				dynamicRtsApproach?.UpdateCorrespondenceModel(coverageResults);
			}

			var automatedTestsProcessor = testProcessor as IAutomatedTestsExecutor<TTestCase>;
			if (automatedTestsProcessor != null)
			{
				var testResults = automatedTestsProcessor.GetResults();
				ReportFinalResults(testResults, resultBuilder);
			}
			return resultBuilder.ToString();
		}

	    public List<TTestCase> ImpactedTests { get; private set; }

        public void NotifyImpactedTest(object sender, ImpactedTestEventArgs<TTestCase> args)
        {
	        var impactedTest = args.TestCase;

			Debug.WriteLine($"Impacted Test: {impactedTest.Id}");
            ImpactedTests.Add(impactedTest);
        }

        private void ReportFinalResults(IEnumerable<ITestCaseResult<TTestCase>> results, StringBuilder resultBuilder)
        {
			resultBuilder.AppendLine();
			resultBuilder.AppendLine("Final more detailed Test Results:");

            var testCaseResults = results as IList<ITestCaseResult<TTestCase>> ?? results.ToList();

            if (File.Exists("Error.log"))
            {
                File.Delete("Error.log");
            }

            using (var errorLog = File.Open("Error.log", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter logWriter = new StreamWriter(errorLog))
                {
                    logWriter.WriteLine("Failed Tests:");

                    foreach (var result in testCaseResults)
                    {
                        ReportTestResult(result, logWriter, resultBuilder);
                    }
                }
            }

            int numberOfTestsNotPassed = testCaseResults.Count(x => x.Outcome != TestCaseResultType.Passed);


			resultBuilder.AppendLine();
			resultBuilder.AppendLine(numberOfTestsNotPassed == 0
                ? $"All {testCaseResults.Count} tests passed!"
                : $"{numberOfTestsNotPassed} of {testCaseResults.Count} did not pass!");
        }

        private void ReportTestResult(ITestCaseResult<TTestCase> result, StreamWriter logWriter, StringBuilder resultBuilder)
        {
			resultBuilder.AppendLine($"{result.TestCaseId}: {result.Outcome}");
            if (result.Outcome != TestCaseResultType.Passed)
            {
                logWriter.WriteLine($"{result.TestCaseId}: {result.Outcome} Message: {result.ErrorMessage} StackTrace: {result.StackTrace}");
            }

            int i = 0;
            foreach (var childResult in result.ChildrenResults)
            {
				resultBuilder.Append($"Data Row {i} - ");
                ReportTestResult(childResult, logWriter, resultBuilder);
                i++;
            }
        }
    }
}