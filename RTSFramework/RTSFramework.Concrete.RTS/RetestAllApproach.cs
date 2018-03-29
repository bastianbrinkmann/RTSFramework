using System.Collections.Generic;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.RTSApproach;

namespace RTSFramework.RTSApproaches.Dynamic
{
    public class RetestAllApproach<TP, TPe, TTc> : RTSApproachBase<TP, TPe, TTc> where TPe : IProgramModelElement where TTc : ITestCase where TP : IProgramModel
    {

        public override void ExecuteRTS(IEnumerable<TTc> testCases, StructuralDelta<TP, TPe> delta)
        {
            foreach (TTc testcase in testCases)
            {
                ReportToAllListeners(testcase);
            }
        }
    }
}