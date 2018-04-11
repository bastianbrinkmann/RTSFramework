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
    public class CSharpProgramModelFileRTSController<TTc> 
        where TTc : ITestCase
    {
        private readonly Func<DiscoveryType, GranularityLevel, IOfflineDeltaDiscoverer> discovererFactory;
        private readonly Func<ProcessingType, ITestProcessor<TTc>> testProcessorFactory;
        private readonly ITestsDiscoverer<TTc> testsDiscoverer;
        private readonly Func<RTSApproachType, IRTSApproach<TTc>> rtsApproachFactory;
        private readonly CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter;

        public CSharpProgramModelFileRTSController(
			Func<DiscoveryType, GranularityLevel, IOfflineDeltaDiscoverer> discovererFactory,
            Func<ProcessingType, ITestProcessor<TTc>> testProcessorFactory,
            ITestsDiscoverer<TTc> testsDiscoverer,
            Func<RTSApproachType, IRTSApproach<TTc>> rtsApproachFactory,
			CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter)
        {
            this.discovererFactory = discovererFactory;
            this.testProcessorFactory = testProcessorFactory;
            this.testsDiscoverer = testsDiscoverer;
            this.rtsApproachFactory = rtsApproachFactory;
            this.assembliesAdapter = assembliesAdapter;
        }

        private StructuralDelta PerformDeltaDiscovery(RunConfiguration configuration)
        {
            var deltaDiscoverer = discovererFactory(configuration.DiscoveryType, configuration.GranularityLevel);

            StructuralDelta delta = default(StructuralDelta);
            DebugStopWatchTracker.ReportNeededTimeOnDebug(
                () => delta = deltaDiscoverer.Discover(configuration.OldProgramModel, configuration.NewProgramModel), "DeltaDiscovery");

            return delta;
        }

	    private const string Cancelled = "Cancelled";
		public async Task<string> ExecuteImpactedTests(RunConfiguration configuration, CancellationToken token)
		{
			StringBuilder resultBuilder = new StringBuilder();

			var testProcessor = testProcessorFactory(configuration.ProcessingType);
			var rtsApproach = rtsApproachFactory(configuration.RTSApproachType);

			var parsingResult = await assembliesAdapter.Parse(configuration.AbsoluteSolutionPath, token);
			if (token.IsCancellationRequested)
			{
				resultBuilder.AppendLine(Cancelled);
				return resultBuilder.ToString();
			}

			//TODO Filtering of Test dlls?
			testsDiscoverer.Sources = parsingResult.Select(x => x.AbsolutePath).Where(x => x.EndsWith("Test.dll"));

			var delta = PerformDeltaDiscovery(configuration);
			if (token.IsCancellationRequested)
			{
				resultBuilder.AppendLine(Cancelled);
				return resultBuilder.ToString();
			}

			IEnumerable<TTc> allTests = null;
			DebugStopWatchTracker.ReportNeededTimeOnDebug(() => allTests = testsDiscoverer.GetTestCases(),
				"TestsDiscovery");
			if (token.IsCancellationRequested)
			{
				resultBuilder.AppendLine(Cancelled);
				return resultBuilder.ToString();
			}
			//TODO Filtering of tests
			//var defaultCategory = allTests.Where(x => x.Categories.Any(y => y == "Default"));

			impactedTests = new List<TTc>();
			rtsApproach.ImpactedTest += NotifyImpactedTest;
			DebugStopWatchTracker.ReportNeededTimeOnDebug(() => rtsApproach.ExecuteRTS(allTests, delta, token),
				"RTSApproach");
			rtsApproach.ImpactedTest -= NotifyImpactedTest;

			resultBuilder.AppendLine($"{impactedTests.Count} Tests impacted");
			if (token.IsCancellationRequested)
			{
				resultBuilder.AppendLine(Cancelled);
				return resultBuilder.ToString();
			}

			await DebugStopWatchTracker.ReportNeededTimeOnDebug(testProcessor.ProcessTests(impactedTests, token), "ProcessingOfImpactedTests");
			if (token.IsCancellationRequested)
			{
				resultBuilder.AppendLine(Cancelled);
				return resultBuilder.ToString();
			}

			var processorWithCoverageCollection = testProcessor as IAutomatedTestsExecutorWithCoverageCollection<TTc>;
			var coverageResults = processorWithCoverageCollection?.GetCollectedCoverageData();
			if (coverageResults != null)
			{
				var dynamicRtsApproach = rtsApproach as DynamicRTSApproach<TTc>;
				dynamicRtsApproach?.UpdateCorrespondenceModel(coverageResults);
			}

			var automatedTestsProcessor = testProcessor as IAutomatedTestsExecutor<TTc>;
			if (automatedTestsProcessor != null)
			{
				var testResults = automatedTestsProcessor.GetResults();
				ReportFinalResults(testResults, resultBuilder);
			}
			return resultBuilder.ToString();
		}

	    private List<TTc> impactedTests;

        public void NotifyImpactedTest(object sender, ImpactedTestEventArgs<TTc> args)
        {
	        var impactedTest = args.TestCase;

			Debug.WriteLine($"Impacted Test: {impactedTest.Id}");
            impactedTests.Add(impactedTest);
        }

        private void ReportFinalResults(IEnumerable<ITestCaseResult<TTc>> results, StringBuilder resultBuilder)
        {
			resultBuilder.AppendLine();
			resultBuilder.AppendLine("Final more detailed Test Results:");

            var testCaseResults = results as IList<ITestCaseResult<TTc>> ?? results.ToList();

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

        private void ReportTestResult(ITestCaseResult<TTc> result, StreamWriter logWriter, StringBuilder resultBuilder)
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