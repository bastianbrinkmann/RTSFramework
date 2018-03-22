using System.Collections.Generic;
using System.Linq;

namespace RTSFramework.RTSApproaches.Utilities
{
    public class DynamicMapProvider
    {
        private readonly List<TransitiveClosureTestDependencies> maps = new List<TransitiveClosureTestDependencies>();

        public TransitiveClosureTestDependencies GetMapByVersionId(string versionId)
        {
            TransitiveClosureTestDependencies map = maps.SingleOrDefault(x => x.ProgramVersionId == versionId);

            if (map == null)
            {
                map = DynamicMapPersistor.LoadTestCasesToProgramMap(versionId);
                maps.Add(map);
            }

            return map;
        }

        public void UpdateMap(TransitiveClosureTestDependencies map)
        {
            var currentMap = maps.SingleOrDefault(x => x.ProgramVersionId == map.ProgramVersionId);

            if (currentMap != null)
            {
                maps.Remove(currentMap);
            }

            DynamicMapPersistor.PersistTestCasestoProgramMap(map);
            maps.Add(map);
        }
    }
}