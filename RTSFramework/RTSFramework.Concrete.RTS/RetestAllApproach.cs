using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.RTSApproaches.Concrete
{
    public class RetestAllApproach<TD, TPe, TP, TTc> : IRTSApproach<TD, TPe, TP, TTc> where TD : IDelta<TPe, TP> where TPe : IProgramElement where TP : IProgram where TTc : ITestCase
    {
        public System.Collections.Generic.IEnumerable<TTc> PerformRTS(System.Collections.Generic.IEnumerable<TTc> testCases, TD delta)
        {
            return testCases;
        }
    }
}