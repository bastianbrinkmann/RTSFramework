using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.RTSApproach;
using RTSFramework.RTSApproaches.CorrespondenceModel;

namespace RTSFramework.RTSApproaches.Dynamic
{
    public class DynamicRTSApproach<TTc> : IRTSApproach<TTc> where TTc : ITestCase
    {
		public event EventHandler<ImpactedTestEventArgs<TTc>> ImpactedTest;

		private readonly CorrespondenceModelManager correspondenceModelManager;
        public DynamicRTSApproach(CorrespondenceModelManager correspondenceModelManager)
        {
            this.correspondenceModelManager = correspondenceModelManager;
        }

        private IList<TTc> allTests;
        private StructuralDelta currentDelta;

        public void ExecuteRTS(IEnumerable<TTc> testCases, StructuralDelta delta, CancellationToken cancellationToken = default(CancellationToken))
        {
            allTests = testCases as IList<TTc> ?? testCases.ToList();
            currentDelta = delta;

            var correspondenceModel = correspondenceModelManager.GetCorrespondenceModel(delta.SourceModel.VersionId, delta.SourceModel.GranularityLevel);

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
	                    ImpactedTest?.Invoke(this, new ImpactedTestEventArgs<TTc>(testcase));
                    }
                }
                else
                {
					//Unknown testcase - considered as new testcase so impacted
					ImpactedTest?.Invoke(this, new ImpactedTestEventArgs<TTc>(testcase));
				}
            }
        }

        public void UpdateCorrespondenceModel(CoverageData coverageData)
        {
            correspondenceModelManager.UpdateCorrespondenceModel(coverageData, currentDelta, currentDelta.TargetModel.GranularityLevel, allTests);
        }
    }
}