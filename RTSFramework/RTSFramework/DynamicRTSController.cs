
using System.Collections.Generic;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Core
{
	public class DynamicRTSController<TD, TPe, TP, TTc> : IRTSListener<TTc> where TD : IDelta<TPe, TP> where TPe : IProgramElement where TP : IProgram where TTc : ITestCase
	{
	    private readonly IOfflineDeltaDiscoverer<TP, TPe, TD> deltaDiscoverer;
	    private readonly IAutomatedTestFrameworkWithMapUpdating<TTc> testFramework;
	    private readonly IRTSApproach<TD, TPe, TP, TTc> rtsApproach;

        public DynamicRTSController(IOfflineDeltaDiscoverer<TP, TPe, TD> deltaDiscoverer, IAutomatedTestFrameworkWithMapUpdating<TTc> testFramework, IRTSApproach<TD, TPe, TP, TTc> rtsApproach)
        {
            this.deltaDiscoverer = deltaDiscoverer;
            this.testFramework = testFramework;
            this.rtsApproach = rtsApproach;
        }

	    public IEnumerable<ITestCaseResult<TTc>> ExecuteImpactedTests(TP oldVersion, TP newVersion)
	    {
	        TD delta = deltaDiscoverer.Discover(oldVersion, newVersion);
	        IEnumerable<TTc> allTests = testFramework.GetTestCases();

            rtsApproach.RegisterImpactedTestObserver(this);
	        rtsApproach.StartRTS(allTests, delta);
            rtsApproach.UnregisterImpactedTestObserver(this);
            
            testFramework.SetSourceAndTargetVersion(oldVersion.VersionId, newVersion.VersionId);
            return testFramework.ExecuteTests(impactedTests);
	    }

        private readonly List<TTc> impactedTests = new List<TTc>();

	    public void NotifyImpactedTest(TTc impactedTest)
	    {
            impactedTests.Add(impactedTest);
        }
	}
}
