using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Core;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.RTSApproaches.Dynamic
{
	public class DynamicRTS<TModel, TModelElement, TTestCase> : TestSelectorBase<TModel, StructuralDelta<TModel, TModelElement>, TTestCase, CorrespondenceModel.Models.CorrespondenceModel>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TModelElement : IProgramModelElement
	{
		public override event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;

		public DynamicRTS(IDataStructureProvider<CorrespondenceModel.Models.CorrespondenceModel, TModel> correspondenceModelProvider) : base(correspondenceModelProvider)
		{
		}

		protected override void SelectTests(CorrespondenceModel.Models.CorrespondenceModel correspondenceModel, IEnumerable<TTestCase> testCases, StructuralDelta<TModel, TModelElement> delta, CancellationToken cancellationToken)
		{
			allTests = testCases as IList<TTestCase> ?? testCases.ToList();
			currentDelta = delta;

			foreach (var testcase in allTests)
			{
				cancellationToken.ThrowIfCancellationRequested();

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

		private IList<TTestCase> allTests;
		private StructuralDelta<TModel, TModelElement> currentDelta;

		public override void UpdateInternalDataStructure(ITestProcessingResult processingResult, CancellationToken token)
		{
			var codeCoverageResult = processingResult as IProcessingResultWithCodeCoverage;

			if (codeCoverageResult != null)
			{
				var oldModel = DataStructureProvider.GetDataStructureForProgram(currentDelta.SourceModel, token);
				var newModel = oldModel.CloneModel(currentDelta.TargetModel.VersionId);
				newModel.UpdateByNewLinks(GetLinksByCoverageData(codeCoverageResult.CoverageData, currentDelta.TargetModel));
				newModel.RemoveDeletedTests(allTests.Select(x => x.Id));

				DataStructureProvider.PersistDataStructure(newModel);
			}
		}

		private Dictionary<string, HashSet<string>> GetLinksByCoverageData(CoverageData coverageData, IProgramModel targetModel)
		{
			var links = coverageData.CoverageDataEntries.Select(x => x.TestCaseId).Distinct().ToDictionary(x => x, x => new HashSet<string>());

			foreach (var coverageEntry in coverageData.CoverageDataEntries)
			{
				if (targetModel.GranularityLevel == GranularityLevel.Class)
				{
					if (!links[coverageEntry.TestCaseId].Contains(coverageEntry.ClassName))
					{
						links[coverageEntry.TestCaseId].Add(coverageEntry.ClassName);
					}
					else
					{
						var relativePath = RelativePathHelper.GetRelativePath(targetModel, coverageEntry.FileName);
						if (!links[coverageEntry.TestCaseId].Contains(relativePath))
						{
							links[coverageEntry.TestCaseId].Add(relativePath);
						}
					}
				}
			}
			return links;
		}
	}
}