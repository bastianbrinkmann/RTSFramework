using System.Collections.Generic;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Core
{
    public abstract class RTSApproachBase<TD, TPe, TP, TTc> : IRTSApproach<TD, TPe, TP, TTc> where TD : IDelta<TPe, TP> where TTc : ITestCase where TPe : IProgramElement where TP : IProgram
    {
        protected readonly List<IRTSListener<TTc>> Listeners = new List<IRTSListener<TTc>>();

        public void RegisterImpactedTestObserver(IRTSListener<TTc> listener)
        {
            Listeners.Add(listener);
        }

        public void UnregisterImpactedTestObserver(IRTSListener<TTc> listener)
        {
            Listeners.Remove(listener);
        }

        protected void ReportToAllListeners(TTc impactedTest)
        {
            foreach (var listener in Listeners)
            {
                listener.NotifyImpactedTest(impactedTest);
            }
        }

        public abstract void StartRTS(IEnumerable<TTc> testCases, TD delta);
    }
}