using System;
using System.Collections.Generic;
using System.Linq;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.Delta;
using RTSFramework.Core;
using RTSFramework.RTSApproaches.Utilities;

namespace RTSFramework.RTSApproaches.Concrete
{
    public class DynamicRTSApproach<TPe, TTc> : RTSApproachBase<TPe, TTc> where TTc : ITestCase where TPe : IProgramModelElement
    {
        public override void ExecuteRTS(IEnumerable<TTc> testCases, StructuralDelta<TPe> delta)
        {
            var map = DynamicMapDictionary.GetMapByVersionId(delta.SourceModelId);

            var allTestcases = testCases as IList<TTc> ?? testCases.ToList();

            //TODO: Iterate over tests required as there could be new tests
            foreach (var testcase in allTestcases)
            {
                HashSet<string> linkedElements;
                if (map.TestCaseToProgramElementsMap.TryGetValue(testcase.Id, out linkedElements))
                {
                    if (delta.ChangedElements.Any(x => linkedElements.Any(y => x.Id.Equals(y, StringComparison.Ordinal))) || 
                        delta.DeletedElements.Any(x => linkedElements.Any(y => x.Id.Equals(y, StringComparison.Ordinal))))
                    {
                        ReportToAllListeners(testcase);
                    }
                }
                else
                {
                    //Unknown testcase - considered as new testcase so impacted
                    ReportToAllListeners(testcase);
                }
            }
        }
    }
}