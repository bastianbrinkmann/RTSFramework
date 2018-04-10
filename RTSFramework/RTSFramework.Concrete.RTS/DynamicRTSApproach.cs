using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.RTSApproach;
using RTSFramework.RTSApproaches.CorrespondenceModel;

namespace RTSFramework.RTSApproaches.Dynamic
{
    public class DynamicRTSApproach<TP, TPe, TTc> : RTSApproachBase<TP, TPe, TTc> where TTc : ITestCase where TPe : IProgramModelElement where TP : IProgramModel
    {
        private readonly CorrespondenceModelManager correspondenceModelManager;
        public DynamicRTSApproach(CorrespondenceModelManager correspondenceModelManager)
        {
            this.correspondenceModelManager = correspondenceModelManager;
        }

        private IList<TTc> allTests;
        private StructuralDelta<TP, TPe> currentDelta;

        public override void ExecuteRTS(IEnumerable<TTc> testCases, StructuralDelta<TP, TPe> delta, CancellationToken cancellationToken = default(CancellationToken))
        {
            allTests = testCases as IList<TTc> ?? testCases.ToList();
            currentDelta = delta;

            var correspondenceModel = correspondenceModelManager.GetCorrespondenceModel(delta.SourceModel.VersionId, GetGranularityLevel());

            //TODO: Iterate over tests required as there could be new tests
            foreach (var testcase in allTests)
            {
	            if (cancellationToken.IsCancellationRequested)
	            {
		            return;
	            }

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

        //TODO Somewhere else and cleaner
        private GranularityLevel GetGranularityLevel()
        {
            if (typeof(TPe).Name == "CSharpClassElement")
            {
                return GranularityLevel.Class;
            }

            return GranularityLevel.File;
        }

        public void UpdateCorrespondenceModel(CoverageData coverageData)
        {
            correspondenceModelManager.UpdateCorrespondenceModel(coverageData, currentDelta, GetGranularityLevel(), allTests);
        }
    }
}