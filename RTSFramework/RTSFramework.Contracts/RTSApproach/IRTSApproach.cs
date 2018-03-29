using System.Collections.Generic;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.RTSApproach
{
    public interface IRTSApproach<TP, TPe, TTc>where TTc : ITestCase where TPe : IProgramModelElement where TP : IProgramModel
    {
        void RegisterImpactedTestObserver(IRTSListener<TTc> listener);

        void UnregisterImpactedTestObserver(IRTSListener<TTc> listener);

        void ExecuteRTS(IEnumerable<TTc> testCases, StructuralDelta<TP, TPe> delta);

    }
}