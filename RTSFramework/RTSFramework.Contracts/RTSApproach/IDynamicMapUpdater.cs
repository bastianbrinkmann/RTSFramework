using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts.RTSApproach
{
    public interface IDynamicMapUpdater
    {
        void UpdateDynamicMap(ICoverageData coverageData, string oldVersionId, string newVersionId, IList<string> allTests);
    }
}