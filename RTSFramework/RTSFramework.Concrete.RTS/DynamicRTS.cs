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

		private CorrespondenceModel.Models.CorrespondenceModel currentCorrespondenceModel;
		private StructuralDelta<TModel, TModelElement> currentDelta;

		public async Task<IList<TTestCase>> SelectTests(IList<TTestCase> testCases, StructuralDelta<TModel, TModelElement> delta, CancellationToken cancellationToken)
		{
			currentCorrespondenceModel = await correspondenceModelProvider.GetDataStructureForProgram(delta.OldModel, cancellationToken);
			currentDelta = delta;

			IList<TTestCase> impactedTests = new List<TTestCase>();

			foreach (var testcase in testCases)
			{
				cancellationToken.ThrowIfCancellationRequested();

				HashSet<string> linkedElements;
				if (currentCorrespondenceModel.CorrespondenceModelLinks.TryGetValue(testcase.Id, out linkedElements))
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

		public IResponsibleChangesProvider GetResponsibleChangesProvider()
		{
			return new DynamicRTSResponsibleChangesProvider(currentDelta, currentCorrespondenceModel);
		}

		private class DynamicRTSResponsibleChangesProvider : IResponsibleChangesProvider
		{
			private readonly StructuralDelta<TModel, TModelElement> delta;
			private readonly CorrespondenceModel.Models.CorrespondenceModel correspondenceModel;

			public DynamicRTSResponsibleChangesProvider(StructuralDelta<TModel, TModelElement> delta,
				CorrespondenceModel.Models.CorrespondenceModel correspondenceModel)
			{
				this.delta = delta;
				this.correspondenceModel = correspondenceModel;
			}

			public IList<string> GetResponsibleChangesForImpactedTest(string testCaseId)
			{
				if (correspondenceModel.CorrespondenceModelLinks.ContainsKey(testCaseId))
				{
					var linksOfTestcase = correspondenceModel.CorrespondenceModelLinks[testCaseId];
					return linksOfTestcase.Where(x => delta.AddedElements.Any(y => y.Id == x) ||
													  delta.ChangedElements.Any(y => y.Id == x) ||
													  delta.DeletedElements.Any(y => y.Id == x)).ToList();
				}

				return new List<string>(new []{"New Test"});
			}
		}
	}
}