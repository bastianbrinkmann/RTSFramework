using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
    public interface IRTSListener<TTc> where TTc : ITestCase
    {
        void NotifyImpactedTest(TTc impactedTest);
    }
}