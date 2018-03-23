using System.Collections.Generic;
using System.Linq;

namespace RTSFramework.RTSApproaches.Utilities
{
    public class TransitiveClosureTestDependencies
    {
        public string ProgramVersionId { get; set; }

        public Dictionary<string, HashSet<string>> TransitiveClosureTestsToProgramElements { get; set; }
            = new Dictionary<string, HashSet<string>>();

        public TransitiveClosureTestDependencies CloneMap(string newId)
        {
            var clone = new Dictionary<string, HashSet<string>>();
            foreach (KeyValuePair<string, HashSet<string>> testcaseRelatedElements in TransitiveClosureTestsToProgramElements)
            {
                clone.Add(testcaseRelatedElements.Key, new HashSet<string>(testcaseRelatedElements.Value));
            }

            return new TransitiveClosureTestDependencies {ProgramVersionId = newId, TransitiveClosureTestsToProgramElements = clone};
        }

        public void UpdateByNewPartialMap(Dictionary<string, HashSet<string>> newMap)
        {
            foreach (KeyValuePair<string, HashSet<string>> testMap in newMap)
            {
                if (!TransitiveClosureTestsToProgramElements.ContainsKey(testMap.Key))
                {
                    TransitiveClosureTestsToProgramElements.Add(testMap.Key, testMap.Value);
                }
                else
                {
                    TransitiveClosureTestsToProgramElements[testMap.Key] = testMap.Value;
                }
            }
        }

        public void RemoveDeletedTests(IEnumerable<string> allTests)
        {
            var deletedTests = TransitiveClosureTestsToProgramElements.Where(x => !allTests.Contains(x.Key)).Select(x => x.Key);

            foreach (var deletedTest in deletedTests)
            {
                TransitiveClosureTestsToProgramElements.Remove(deletedTest);
            }
        }
    }
}