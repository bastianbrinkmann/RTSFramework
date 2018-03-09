
using System.Collections.Generic;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Core
{
	public class OfflineController<TD, TPe, TP, TTc> where TD : IDelta<TPe> where TPe : IProgramElement where TP : IProgram where TTc : ITestCase
	{
	    private readonly IOfflineDeltaDiscoverer<TP, TPe, TD> deltaDiscoverer;
	    private readonly IAutomatedTestFramework<TTc> testFramework;
	    private readonly IRTSApproach<TD, TPe, TTc> rtsApproach;

        public OfflineController(IOfflineDeltaDiscoverer<TP, TPe, TD> deltaDiscoverer, IAutomatedTestFramework<TTc> testFramework, IRTSApproach<TD, TPe, TTc> rtsApproach)
        {
            this.deltaDiscoverer = deltaDiscoverer;
            this.testFramework = testFramework;
            this.rtsApproach = rtsApproach;
        }

	    public IEnumerable<ITestCaseResult<TTc>> ExecuteImpactedTests(TP oldVersion, TP newVersion)
	    {
	        TD delta = deltaDiscoverer.Discover(oldVersion, newVersion);
	        IEnumerable<TTc> allTests = testFramework.GetTestCases();

	        IEnumerable<TTc> impactedTests = rtsApproach.PerformRTS(allTests, delta);

	        return testFramework.ExecuteTests(impactedTests);
	    }
	}
}
