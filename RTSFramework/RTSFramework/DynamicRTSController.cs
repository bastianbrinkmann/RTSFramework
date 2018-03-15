
using System.Collections.Generic;
using System.Diagnostics;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Utilities;

namespace RTSFramework.Core
{
	public class DynamicRTSController<TD, TPe, TP, TTc> : IRTSListener<TTc> where TD : IDelta<TPe, TP> where TPe : IProgramElement where TP : IProgram where TTc : ITestCase
	{
	    private readonly IOfflineDeltaDiscoverer<TP, TPe, TD> deltaDiscoverer;
	    private readonly IAutomatedTestFrameworkWithCoverageCollection<TTc> testFramework;
	    private readonly IRTSApproach<TD, TPe, TP, TTc> rtsApproach;

        public DynamicRTSController(IOfflineDeltaDiscoverer<TP, TPe, TD> deltaDiscoverer, IAutomatedTestFrameworkWithCoverageCollection<TTc> testFramework, IRTSApproach<TD, TPe, TP, TTc> rtsApproach)
        {
            this.deltaDiscoverer = deltaDiscoverer;
            this.testFramework = testFramework;
            this.rtsApproach = rtsApproach;
        }

	    public IEnumerable<ITestCaseResult<TTc>> ExecuteImpactedTests(TP oldVersion, TP newVersion)
	    {
            TD delta = default(TD);
	        ConsoleStopWatchTracker.ReportNeededTimeOnConsole(
	            () => delta = deltaDiscoverer.Discover(oldVersion, newVersion), "DeltaDiscovery");
	        ;
	        IEnumerable<TTc> allTests = null;
	        ConsoleStopWatchTracker.ReportNeededTimeOnConsole(() => allTests = testFramework.GetTestCases(),
	            "GettingTestcases");

            rtsApproach.RegisterImpactedTestObserver(this);
            ConsoleStopWatchTracker.ReportNeededTimeOnConsole(() => rtsApproach.StartRTS(allTests, delta),
                "RTSApproach");
            rtsApproach.UnregisterImpactedTestObserver(this);

	        IEnumerable<ITestCaseResult<TTc>> testResults = null;
	        ConsoleStopWatchTracker.ReportNeededTimeOnConsole(
	            () => testResults = testFramework.ExecuteTests(impactedTests), "ExecuteWithCodeCoverage");

	        var coverageResults = testFramework.GetCollectedCoverageData();

            DynamicMapDictionary.UpdateMap(new TestCasesToProgramMap
            {
                ProgramVersionId = newVersion.VersionId,
                TestCaseToProgramElementsMap = coverageResults.TestCaseToProgramElementsMap
            });

	        return testResults;
	    }

        private readonly List<TTc> impactedTests = new List<TTc>();

	    public void NotifyImpactedTest(TTc impactedTest)
	    {
            impactedTests.Add(impactedTest);
        }
	}
}
