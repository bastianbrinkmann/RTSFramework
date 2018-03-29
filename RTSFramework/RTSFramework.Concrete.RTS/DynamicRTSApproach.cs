using System;
using System.Collections.Generic;
using System.Linq;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core;
using RTSFramework.Core.RTSApproach;
using RTSFramework.RTSApproaches.CorrespondenceModel;

namespace RTSFramework.RTSApproaches.Concrete
{
    public class DynamicRTSApproach<TPe, TTc> : RTSApproachBase<TPe, TTc> where TTc : ITestCase where TPe : IProgramModelElement
    {
        private readonly CorrespondenceModelManager correspondenceModelManager;
        public DynamicRTSApproach(CorrespondenceModelManager correspondenceModelManager)
        {
            this.correspondenceModelManager = correspondenceModelManager;
        }

        private IList<TTc> allTests;
        private StructuralDelta<TPe> currentDelta;

        public override void ExecuteRTS(IEnumerable<TTc> testCases, StructuralDelta<TPe> delta)
        {
            allTests = testCases as IList<TTc> ?? testCases.ToList();
            currentDelta = delta;

            var correspondenceModel = correspondenceModelManager.GetCorrespondenceModel(delta.SourceModelId);

            //TODO: Iterate over tests required as there could be new tests
            foreach (var testcase in allTests)
            {
                HashSet<string> linkedElements;
                if (correspondenceModel.CorrespondenceModelLinks.TryGetValue(testcase.Id, out linkedElements))
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

        public void UpdateCorrespondenceModel(ICoverageData coverageData)
        {
            correspondenceModelManager.UpdateCorrespondenceModel(coverageData, currentDelta.SourceModelId, currentDelta.TargetModelId, allTests);
        }
    }
}