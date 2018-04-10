using System.Collections.Generic;
using System.Threading;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.RTSApproach;

namespace RTSFramework.Core.RTSApproach
{
    public abstract class RTSApproachBase<TP, TPe, TTc> : IRTSApproach<TP, TPe, TTc> where TTc : ITestCase where TPe : IProgramModelElement where TP : IProgramModel
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

        public abstract void ExecuteRTS(IEnumerable<TTc> testCases, StructuralDelta<TP, TPe> delta, CancellationToken cancellationToken = default(CancellationToken));
    }
}