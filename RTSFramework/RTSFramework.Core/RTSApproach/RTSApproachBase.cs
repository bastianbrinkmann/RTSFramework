using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.Delta;
using RTSFramework.Contracts.RTSApproach;

namespace RTSFramework.Core.RTSApproach
{
    public abstract class RTSApproachBase<TPe, TTc> : IRTSApproach<TPe, TTc> where TTc : ITestCase where TPe : IProgramModelElement
    {
        private readonly List<IRTSListener<TTc>> listeners = new List<IRTSListener<TTc>>();

        public void RegisterImpactedTestObserver(IRTSListener<TTc> listener)
        {
            listeners.Add(listener);
        }

        public void UnregisterImpactedTestObserver(IRTSListener<TTc> listener)
        {
            listeners.Remove(listener);
        }

        protected void ReportToAllListeners(TTc impactedTest)
        {
            foreach (var listener in listeners)
            {
                listener.NotifyImpactedTest(impactedTest);
            }
        }

        public abstract void ExecuteRTS(IEnumerable<TTc> testCases, StructuralDelta<TPe> delta);
    }
}