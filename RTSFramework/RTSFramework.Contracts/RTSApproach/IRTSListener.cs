using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts.RTSApproach
{
    public interface IRTSListener<TTc> where TTc : ITestCase
    {
        void NotifyImpactedTest(TTc impactedTest);
    }
}