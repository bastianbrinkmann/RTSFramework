using System.Collections.Generic;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core;

namespace RTSFramework.RTSApproaches.Concrete
{
    public class RetestAllApproach<TD, TPe, TP, TTc> : RTSApproachBase<TD, TPe, TP, TTc> where TD : IDelta<TPe, TP> where TPe : IProgramElement where TP : IProgram where TTc : ITestCase
    {

        public override void StartRTS(IEnumerable<TTc> testCases, TD delta)
        {
            foreach (TTc testcase in testCases)
            {
                ReportToAllListeners(testcase);
            }
        }
    }
}