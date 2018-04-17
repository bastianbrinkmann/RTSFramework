using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.RTSApproach;

namespace RTSFramework.RTSApproaches.Dynamic
{
	public class DynamicRTS<TModel, TModelElement, TTestCase> : IDynamicRTSApproach<TModel, StructuralDelta<TModel, TModelElement>, TTestCase, CorrespondenceModel.Models.CorrespondenceModel>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TModelElement : IProgramModelElement
	{
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;

		public void ExecuteRTS(IEnumerable<TTestCase> testCases, StructuralDelta<TModel, TModelElement> delta, CancellationToken cancellationToken)
		{
			var allTests = testCases as IList<TTestCase> ?? testCases.ToList();
			foreach (var testcase in allTests)
			{
				cancellationToken.ThrowIfCancellationRequested();

				HashSet<string> linkedElements;
				if (CorrespondenceModel.CorrespondenceModelLinks.TryGetValue(testcase.Id, out linkedElements))
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

		public CorrespondenceModel.Models.CorrespondenceModel CorrespondenceModel { get; set; }
	}
}