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
    public class DynamicRTS<TModel, TModelElement, TTestCase> : IRTSApproach<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>, IDynamicRTSApproach
		where TTestCase : ITestCase 
		where TModel : IProgramModel 
		where TModelElement : IProgramModelElement
    {
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;

		private readonly CorrespondenceModelManager correspondenceModelManager;
        public DynamicRTS(CorrespondenceModelManager correspondenceModelManager)
        {
            this.correspondenceModelManager = correspondenceModelManager;
        }

        private IList<TTestCase> allTests;
        private StructuralDelta<TModel, TModelElement> currentDelta;

        public void ExecuteRTS(IEnumerable<TTestCase> testCases, StructuralDelta<TModel, TModelElement> delta, CancellationToken cancellationToken = default(CancellationToken))
        {
            allTests = testCases as IList<TTestCase> ?? testCases.ToList();
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
	                    ImpactedTest?.Invoke(this, new ImpactedTestEventArgs<TTestCase>(testcase));
                    }
                }
                else
                {
					//Unknown testcase - considered as new testcase so impacted
					ImpactedTest?.Invoke(this, new ImpactedTestEventArgs<TTestCase>(testcase));
				}
            }
        }

        public void UpdateCorrespondenceModel(CoverageData coverageData)
        {
            correspondenceModelManager.UpdateCorrespondenceModel(coverageData, currentDelta, currentDelta.TargetModel.GranularityLevel, allTests);
        }
    }
}