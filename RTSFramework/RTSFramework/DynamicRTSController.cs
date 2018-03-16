using System;
using System.Collections.Generic;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.Delta;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Utilities;

namespace RTSFramework.Core
{
	public class DynamicRTSController<TPeDiscoverer, TPeRTSApproach, TP, TTc> : IRTSListener<TTc> where TPeDiscoverer : IProgramModelElement where TPeRTSApproach : IProgramModelElement where TTc : ITestCase where TP: IProgramModel
	{
	    private readonly IOfflineDeltaDiscoverer<TP, StructuralDelta<TPeDiscoverer>> deltaDiscoverer;
	    private readonly IAutomatedTestFramework<TTc> testFramework;
	    private readonly IRTSApproach<TPeRTSApproach, TTc> rtsApproach;
	    private readonly IDeltaAdapter<TPeDiscoverer, TPeRTSApproach> deltaAdapter;

        public DynamicRTSController(
            IOfflineDeltaDiscoverer<TP, StructuralDelta<TPeDiscoverer>> deltaDiscoverer,
            IAutomatedTestFramework<TTc> testFramework, 
            IRTSApproach<TPeRTSApproach, TTc> rtsApproach, 
            IDeltaAdapter<TPeDiscoverer, TPeRTSApproach> deltaAdapter)
        {
            this.deltaDiscoverer = deltaDiscoverer;
            this.testFramework = testFramework;
            this.rtsApproach = rtsApproach;
            this.deltaAdapter = deltaAdapter;
        }

	    public IEnumerable<ITestCaseResult<TTc>> ExecuteImpactedTests(TP oldVersion, TP newVersion)
	    {
            StructuralDelta<TPeDiscoverer> delta = default(StructuralDelta<TPeDiscoverer>);
	        ConsoleStopWatchTracker.ReportNeededTimeOnConsole(
	            () => delta = deltaDiscoverer.Discover(oldVersion, newVersion), "DeltaDiscovery");
	        
	        IEnumerable<TTc> allTests = null;
	        ConsoleStopWatchTracker.ReportNeededTimeOnConsole(() => allTests = testFramework.GetTestCases(),
	            "GettingTestcases");


	        var convertedDelta = deltaAdapter.Convert(delta);

            rtsApproach.RegisterImpactedTestObserver(this);
            ConsoleStopWatchTracker.ReportNeededTimeOnConsole(() => rtsApproach.ExecuteRTS(allTests, convertedDelta),
                "RTSApproach");
            rtsApproach.UnregisterImpactedTestObserver(this);
            Console.WriteLine($"{impactedTests.Count} Tests impacted");

            IEnumerable<ITestCaseResult<TTc>> testResults = null;
	        ConsoleStopWatchTracker.ReportNeededTimeOnConsole(
	            () => testResults = testFramework.ExecuteTests(impactedTests), "ExecuteWithCodeCoverage");

	        var frameworkWithCoverageCollection = testFramework as IAutomatedTestFrameworkWithCoverageCollection<TTc>;
	        var coverageResults = frameworkWithCoverageCollection?.GetCollectedCoverageData();
	        if (coverageResults != null)
	        {
	            DynamicMapDictionary.UpdateMap(new TestCasesToProgramMap
	            {
	                ProgramVersionId = newVersion.VersionId,
	                TestCaseToProgramElementsMap = coverageResults.TestCaseToProgramElementsMap
	            });
	        }

	        return testResults;
	    }

        private readonly List<TTc> impactedTests = new List<TTc>();

	    public void NotifyImpactedTest(TTc impactedTest)
	    {
            impactedTests.Add(impactedTest);
        }
	}
}
