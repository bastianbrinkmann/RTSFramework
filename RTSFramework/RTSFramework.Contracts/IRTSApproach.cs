using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.Delta;

namespace RTSFramework.Contracts
{
    public interface IRTSApproach<TPe, TTc>where TTc : ITestCase where TPe : IProgramModelElement
    {
        void RegisterImpactedTestObserver(IRTSListener<TTc> listener);

        void UnregisterImpactedTestObserver(IRTSListener<TTc> listener);

        void ExecuteRTS(IEnumerable<TTc> testCases, StructuralDelta<TPe> delta);

    }
}