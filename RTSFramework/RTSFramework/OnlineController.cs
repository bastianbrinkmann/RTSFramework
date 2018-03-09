
using System.Collections.Generic;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Core
{
	public class OnlineController<TD, TPe, TP, TTc> where TD : IDelta<TPe, TP> where TPe : IProgramElement where TP : IProgram where TTc : ITestCase
	{
	    private readonly IOnlineDeltaDiscoverer<TP, TPe, TD> deltaDiscoverer;
	    private readonly IAutomatedTestFramework<TTc> testFramework;
	    private readonly IRTSApproach<TD, TPe, TP, TTc> rtsApproach;

        public OnlineController(IOnlineDeltaDiscoverer<TP, TPe, TD> deltaDiscoverer, IAutomatedTestFramework<TTc> testFramework, IRTSApproach<TD, TPe, TP, TTc> rtsApproach)
        {
            this.deltaDiscoverer = deltaDiscoverer;
            this.testFramework = testFramework;
            this.rtsApproach = rtsApproach;
        }

	    public void StartWorking(TP initalProgramVersion)
	    {
	        deltaDiscoverer.StartDiscovery(initalProgramVersion);
	    }

	    public IEnumerable<ITestCaseResult<TTc>> ExecuteCollectedImpactedTests()
	    {
	        TD delta = deltaDiscoverer.GetCurrentDelta();
	        IEnumerable<TTc> allTests = testFramework.GetTestCases();

	        IEnumerable<TTc> impactedTests = rtsApproach.PerformRTS(allTests, delta);

	        return testFramework.ExecuteTests(impactedTests);
	    }
	}
}
