using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Core;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.RTSApproaches.Dynamic
{
	public class DynamicRTS<TModel, TModelElement, TTestCase> : ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TModelElement : IProgramModelElement
	{
		private readonly IDataStructureProvider<CorrespondenceModel.Models.CorrespondenceModel, TModel> correspondenceModelProvider;

		public DynamicRTS(IDataStructureProvider<CorrespondenceModel.Models.CorrespondenceModel, TModel> correspondenceModelProvider)
		{
			this.correspondenceModelProvider = correspondenceModelProvider;
		}

		public async Task<IList<TTestCase>> SelectTests(IList<TTestCase> testCases, StructuralDelta<TModel, TModelElement> delta, CancellationToken cancellationToken)
		{
			var correspondenceModel = await correspondenceModelProvider.GetDataStructureForProgram(delta.OldModel, cancellationToken);

			IList<TTestCase> impactedTests = new List<TTestCase>();

			foreach (var testcase in testCases)
			{
				cancellationToken.ThrowIfCancellationRequested();

				HashSet<string> linkedElements;
				if (correspondenceModel.CorrespondenceModelLinks.TryGetValue(testcase.Id, out linkedElements))
				{
					if (delta.ChangedElements.Any(x => linkedElements.Any(y => x.Id.Equals(y, StringComparison.Ordinal))) ||
						delta.DeletedElements.Any(x => linkedElements.Any(y => x.Id.Equals(y, StringComparison.Ordinal))))
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