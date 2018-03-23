using System.Collections.Generic;
using System.Linq;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.RTSApproaches.Utilities
{
    public class DynamicMapManager
    {
        private readonly List<TransitiveClosureTestDependencies> maps = new List<TransitiveClosureTestDependencies>();

        public TransitiveClosureTestDependencies GetMap(string versionId)
        {
            TransitiveClosureTestDependencies map = maps.SingleOrDefault(x => x.ProgramVersionId == versionId);

            if (map == null)
            {
                map = DynamicMapPersistor.LoadTestCasesToProgramMap(versionId);
                maps.Add(map);
            }

            return map;
        }

        public void UpdateDynamicMap<TTc>(ICoverageData coverageData, string oldVersionId, string newVersionId, IEnumerable<TTc> allTests) where TTc : ITestCase
        {
            var oldMap = GetMap(oldVersionId);
            var newMap = oldMap.CloneMap(newVersionId);
            newMap.UpdateByNewPartialMap(coverageData.TransitiveClosureTestsToProgramElements);
            newMap.RemoveDeletedTests(allTests.Select(x => x.Id));

            UpdateMap(newMap);
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