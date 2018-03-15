using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
    public interface IAutomatedTestFrameworkWithMapUpdating<TTc> : IAutomatedTestFramework<TTc> where TTc : ITestCase
    {
        void SetSourceAndTargetVersion(string sourceVersionId, string targetVersionId);
    }
}