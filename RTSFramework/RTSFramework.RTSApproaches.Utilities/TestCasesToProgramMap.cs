using System.Collections.Generic;

namespace RTSFramework.RTSApproaches.Utilities
{
    public class TestCasesToProgramMap
    {
        public string ProgramVersionId { get; set; }

        public Dictionary<string, HashSet<string>> TestCaseToProgramElementsMap { get; set; }
            = new Dictionary<string, HashSet<string>>();

        public TestCasesToProgramMap CloneMap(string newId)
        {
            var clone = new Dictionary<string, HashSet<string>>();
            foreach (KeyValuePair<string, HashSet<string>> testcaseRelatedElements in TestCaseToProgramElementsMap)
            {
                clone.Add(testcaseRelatedElements.Key, new HashSet<string>(testcaseRelatedElements.Value));
            }

            return new TestCasesToProgramMap {ProgramVersionId = newId, TestCaseToProgramElementsMap = clone};
        }

        public void UpdateByNewPartialMap(Dictionary<string, HashSet<string>> newMap)
        {
            foreach (KeyValuePair<string, HashSet<string>> testMap in newMap)
            {
                if (!TestCaseToProgramElementsMap.ContainsKey(testMap.Key))
                {
                    TestCaseToProgramElementsMap.Add(testMap.Key, testMap.Value);
                }
                else
                {
                    TestCaseToProgramElementsMap[testMap.Key] = testMap.Value;
                }
            }
        }
    }
}