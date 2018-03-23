using System;
using System.Collections.Generic;
using System.Linq;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.Delta;
using RTSFramework.Core;
using RTSFramework.Core.RTSApproach;
using RTSFramework.RTSApproaches.Utilities;

namespace RTSFramework.RTSApproaches.Concrete
{
    public class DynamicRTSApproach<TPe, TTc> : RTSApproachBase<TPe, TTc> where TTc : ITestCase where TPe : IProgramModelElement
    {
        private readonly DynamicMapManager dynamicMapManager;
        public DynamicRTSApproach(DynamicMapManager dynamicMapManager)
        {
            this.dynamicMapManager = dynamicMapManager;
        }

        private IList<TTc> allTests;
        private StructuralDelta<TPe> currentDelta;

        public override void ExecuteRTS(IEnumerable<TTc> testCases, StructuralDelta<TPe> delta)
        {
            allTests = testCases as IList<TTc> ?? testCases.ToList();
            currentDelta = delta;

            var map = dynamicMapManager.GetMap(delta.SourceModelId);

            //TODO: Iterate over tests required as there could be new tests
            foreach (var testcase in allTests)
            {
                HashSet<string> linkedElements;
                if (map.TransitiveClosureTestsToProgramElements.TryGetValue(testcase.Id, out linkedElements))
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

        public void UpdateMap(ICoverageData coverageData)
        {
            dynamicMapManager.UpdateDynamicMap(coverageData, currentDelta.SourceModelId, currentDelta.TargetModelId, allTests);
        }
    }
}