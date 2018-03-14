using System.Collections.Generic;
using System.Linq;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core;
using RTSFramework.RTSApproaches.Utilities;

namespace RTSFramework.RTSApproaches.Concrete
{
    public class DocumentLevelDynamicRTSApproach<TP, TPe, TTc> : IRTSApproach<StructuralDelta<TPe, TP>, TPe, TP, TTc> where TP : IProgram where TTc : ITestCase where TPe : IProgramElement
    {
        public IEnumerable<TTc> PerformRTS(IEnumerable<TTc> testCases, StructuralDelta<TPe, TP> delta)
        {
            var map = DynamicMapPersistor.LoadTestCasesToProgramMap(delta.Source.VersionId);

            var impactedTests = new List<TTc>();

            var allTestcases = testCases as IList<TTc> ?? testCases.ToList();

            //TODO: Iterate over tests or changes? -> discuss in fundamentals
            foreach (var testcase in allTestcases)
            {
                HashSet<string> linkedElements;
                if (map.TestCaseToProgramElementsMap.TryGetValue(testcase.Id, out linkedElements))
                {
                    if (delta.ChangedElements.Any(x => linkedElements.Any(y => x.Id == y)) || 
                        delta.DeletedElements.Any(x => linkedElements.Any(y => x.Id == y)))
                    {
                        impactedTests.Add(testcase);
                    }
                }
                else
                {
                    //Unknown testcase - considered as new testcase so impacted
                    impactedTests.Add(testcase);
                }
            }

            return impactedTests;
        }
    }
}