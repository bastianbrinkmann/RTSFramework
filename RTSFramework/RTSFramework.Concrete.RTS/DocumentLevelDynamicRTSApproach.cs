using System;
using System.Collections.Generic;
using System.Linq;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core;
using RTSFramework.RTSApproaches.Utilities;
using Unity.Attributes;

namespace RTSFramework.RTSApproaches.Concrete
{
    public class DocumentLevelDynamicRTSApproach<TP, TPe, TTc> : RTSApproachBase<StructuralDelta<TPe, TP>, TPe, TP, TTc> where TP : IProgram where TTc : ITestCase where TPe : IProgramElement
    {
        public override void StartRTS(IEnumerable<TTc> testCases, StructuralDelta<TPe, TP> delta)
        {
            var map = DynamicMapDictionary.GetMapByVersionId(delta.Source.VersionId);

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