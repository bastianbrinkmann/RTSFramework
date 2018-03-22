using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.RTSApproach;
using RTSFramework.RTSApproaches.Utilities;

namespace RTSFramework.RTSApproaches.Concrete
{
    public class DynamicMapUpdater : IDynamicMapUpdater
    {
        private readonly DynamicMapProvider dynamicMapProvider;

        public DynamicMapUpdater(DynamicMapProvider dynamicMapProvider)
        {
            this.dynamicMapProvider = dynamicMapProvider;
        }

        public void UpdateDynamicMap(ICoverageData coverageData, string oldVersionId, string newVersionId, IList<string> allTests)
        {
            var oldMap = dynamicMapProvider.GetMapByVersionId(oldVersionId);
            var newMap = oldMap.CloneMap(newVersionId);
            newMap.UpdateByNewPartialMap(coverageData.TransitiveClosureTestsToProgramElements);
            newMap.RemoveDeletedTests(allTests);

            dynamicMapProvider.UpdateMap(newMap);
        }
    }
}