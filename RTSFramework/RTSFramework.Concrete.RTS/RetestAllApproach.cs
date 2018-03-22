using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.Delta;
using RTSFramework.Core;
using RTSFramework.Core.RTSApproach;

namespace RTSFramework.RTSApproaches.Concrete
{
    public class RetestAllApproach<TPe, TTc> : RTSApproachBase<TPe, TTc> where TPe : IProgramModelElement where TTc : ITestCase
    {

        public override void ExecuteRTS(IEnumerable<TTc> testCases, StructuralDelta<TPe> delta)
        {
            foreach (TTc testcase in testCases)
            {
                ReportToAllListeners(testcase);
            }
        }
    }
}