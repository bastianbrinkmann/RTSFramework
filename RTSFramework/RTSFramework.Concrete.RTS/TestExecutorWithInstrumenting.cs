using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.RTSApproaches.Dynamic
{
	public class TestExecutorWithInstrumenting<TModel, TDelta, TTestCase> : ITestExecutor<TTestCase, TDelta, TModel>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TDelta : IDelta<TModel>
	{
		private readonly ITestExecutor<TTestCase, TDelta, TModel> executor;
		private readonly ITestInstrumentor<TModel, TTestCase> instrumentor;
		private readonly IDataStructureProvider<CorrespondenceModel.Models.CorrespondenceModel, TModel> dataStructureProvider;

		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;

		public TestExecutorWithInstrumenting(ITestExecutor<TTestCase, TDelta, TModel> executor,
			ITestInstrumentor<TModel, TTestCase> instrumentor,
			IDataStructureProvider<CorrespondenceModel.Models.CorrespondenceModel, TModel> dataStructureProvider)
		{
			this.executor = executor;
			this.instrumentor = instrumentor;
			this.dataStructureProvider = dataStructureProvider;
		}

		public async Task<ITestExecutionResult<TTestCase>> ProcessTests(IList<TTestCase> impactedTests, IList<TTestCase> allTests, TDelta impactedForDelta, CancellationToken cancellationToken)
		{
			await instrumentor.InstrumentModelForTests(impactedForDelta.TargetModel, impactedTests, cancellationToken);

			executor.TestResultAvailable += TestResultAvailable;
			var result = await executor.ProcessTests(impactedTests, allTests, impactedForDelta, cancellationToken);
			executor.TestResultAvailable -= TestResultAvailable;

			var coverage = instrumentor.GetCoverageData();

			await UpdateCorrespondenceModel(coverage, impactedForDelta, allTests, cancellationToken);

			return result;
		}

		private async Task UpdateCorrespondenceModel(CoverageData coverageData, TDelta currentDelta, IList<TTestCase> allTests, CancellationToken token)
		{
			var oldModel = await dataStructureProvider.GetDataStructureForProgram(currentDelta.SourceModel, token);
			var newModel = oldModel.CloneModel(currentDelta.TargetModel.VersionId);
			newModel.UpdateByNewLinks(GetLinksByCoverageData(coverageData, currentDelta.TargetModel));
			newModel.RemoveDeletedTests(allTests.Select(x => x.Id));

			await dataStructureProvider.PersistDataStructure(newModel);
		}

		private Dictionary<string, HashSet<string>> GetLinksByCoverageData(CoverageData coverageData, IProgramModel targetModel)
		{
			var links = coverageData.CoverageDataEntries.Select(x => x.Item1).Distinct().ToDictionary(x => x, x => new HashSet<string>());

			foreach (var coverageEntry in coverageData.CoverageDataEntries)
			{
				if (targetModel.GranularityLevel == GranularityLevel.Class)
				{
					if (!links[coverageEntry.Item1].Contains(coverageEntry.Item2))
					{
						links[coverageEntry.Item1].Add(coverageEntry.Item2);
					}
				}
				/* TODO Granularity Level File?
				 * 
				 * else if(targetModel.GranularityLevel == GranularityLevel.File)
				{
					if (!coverageEntry.Item2.EndsWith(".cs"))
					{
						continue;
					}
					var relativePath = RelativePathHelper.GetRelativePath(targetModel, coverageEntry.Item2);
					if (!links[coverageEntry.Item1].Contains(relativePath))
					{
						links[coverageEntry.Item1].Add(relativePath);
					}
				}*/
			}
			return links;
		}
	}
}