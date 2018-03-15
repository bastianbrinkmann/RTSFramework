using System.Collections.Generic;
using System.Linq;

namespace RTSFramework.RTSApproaches.Utilities
{
    public static class DynamicMapDictionary
    {
        private static readonly List<TestCasesToProgramMap> maps = new List<TestCasesToProgramMap>();

        public static TestCasesToProgramMap GetMapByVersionId(string versionId)
        {
            TestCasesToProgramMap map = maps.SingleOrDefault(x => x.ProgramVersionId == versionId);

            if (map == null)
            {
                map = DynamicMapPersistor.LoadTestCasesToProgramMap(versionId);
                maps.Add(map);
            }

            return map;
        }

        public static void UpdateMap(TestCasesToProgramMap map)
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