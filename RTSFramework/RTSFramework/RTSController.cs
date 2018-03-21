using System;
using System.Collections.Generic;
using System.Linq;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.Delta;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Utilities;

namespace RTSFramework.Core
{
	public class RTSController<TPeDiscoverer, TPeRTSApproach, TPDiscoverer, TTc> : IRTSListener<TTc> where TPeDiscoverer : IProgramModelElement where TPeRTSApproach : IProgramModelElement where TTc : ITestCase where TPDiscoverer : IProgramModel
	{
	    private readonly IOfflineDeltaDiscoverer<TPDiscoverer, StructuralDelta<TPeDiscoverer>> deltaDiscoverer;
	    private readonly ITestFramework<TTc> testFramework;
        private readonly ITestProcessor<TTc> testProcessor;
        private readonly IRTSApproach<TPeRTSApproach, TTc> rtsApproach;
	    private readonly IDeltaAdapter<TPeDiscoverer, TPeRTSApproach> deltaAdapter;

        public RTSController(
            IOfflineDeltaDiscoverer<TPDiscoverer, StructuralDelta<TPeDiscoverer>> deltaDiscoverer,
            ITestFramework<TTc> testFramework,
            ITestProcessor<TTc> testProcessor,
            IRTSApproach<TPeRTSApproach, TTc> rtsApproach, 
            IDeltaAdapter<TPeDiscoverer, TPeRTSApproach> deltaAdapter)
        {
            this.deltaDiscoverer = deltaDiscoverer;
            this.testFramework = testFramework;
            this.testProcessor = testProcessor;
            this.rtsApproach = rtsApproach;
            this.deltaAdapter = deltaAdapter;
        }

	    public void ExecuteImpactedTests(TPDiscoverer oldVersion, TPDiscoverer newVersion)
	    {
            StructuralDelta<TPeDiscoverer> delta = default(StructuralDelta<TPeDiscoverer>);
	        ConsoleStopWatchTracker.ReportNeededTimeOnConsole(
	            () => delta = deltaDiscoverer.Discover(oldVersion, newVersion), "DeltaDiscovery");
	        
	        IEnumerable<TTc> allTests = null;
	        ConsoleStopWatchTracker.ReportNeededTimeOnConsole(() => allTests = testFramework.GetTestCases(),
	            "GettingTestcases");

			//TODO Filtering of tests
		    //var defaultCategory = allTests.Where(x => x.Categories.Any(y => y == "Default"));

	        var convertedDelta = deltaAdapter.Convert(delta);

            rtsApproach.RegisterImpactedTestObserver(this);
            ConsoleStopWatchTracker.ReportNeededTimeOnConsole(() => rtsApproach.ExecuteRTS(allTests, convertedDelta),
                "RTSApproach");
            rtsApproach.UnregisterImpactedTestObserver(this);
            Console.WriteLine($"{impactedTests.Count} Tests impacted");

	        ConsoleStopWatchTracker.ReportNeededTimeOnConsole(
	            () => testProcessor.ProcessTests(impactedTests), "ProcessingOfImpactedTests");
	        

	        var frameworkWithCoverageCollection = testFramework as IAutomatedTestFrameworkWithCoverageCollection<TTc>;
	        var coverageResults = frameworkWithCoverageCollection?.GetCollectedCoverageData();
	        if (coverageResults != null)
	        {
	            var oldMap = DynamicMapDictionary.GetMapByVersionId(oldVersion.VersionId);
	            var newMap = oldMap.CloneMap(newVersion.VersionId);
                newMap.UpdateByNewPartialMap(coverageResults.TestCaseToProgramElementsMap);
                newMap.RemoveDeletedTests(allTests.Select(x => x.Id).ToList());

	            DynamicMapDictionary.UpdateMap(newMap);
	        }

	        var automatedTestsFramework = testFramework as IAutomatedTestFramework<TTc>;
	        if (automatedTestsFramework != null)
	        {
                var testResults = automatedTestsFramework.GetResults();
                ReportFinalResultsToConsole(testResults);
            }
	    }

        private readonly List<TTc> impactedTests = new List<TTc>();

	    public void NotifyImpactedTest(TTc impactedTest)
	    {
            impactedTests.Add(impactedTest);
        }

        private void ReportFinalResultsToConsole(IEnumerable<ITestCaseResult<TTc>> results)
        {
            Console.WriteLine();
            Console.WriteLine("Final more detailed Test Results:");

            var testCaseResults = results as IList<ITestCaseResult<TTc>> ?? results.ToList();
            foreach (var result in testCaseResults)
            {
                Console.WriteLine($"{result.AssociatedTestCase.Id}: {result.Outcome}");
            }
            int numberOfTestsNotPassed = testCaseResults.Count(x => x.Outcome != TestCaseResultType.Passed);

            Console.WriteLine();
            Console.WriteLine(numberOfTestsNotPassed == 0 ? "All tests passed!" : $"{numberOfTestsNotPassed} of {testCaseResults.Count()} did not pass!");

            Console.ReadKey();
        }
    }
}
