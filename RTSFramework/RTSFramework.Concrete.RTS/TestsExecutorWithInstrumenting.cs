﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.Contracts.Utilities;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.RTSApproaches.Dynamic
{
	public class TestsExecutorWithInstrumenting<TModel, TDelta, TTestCase> : ITestsExecutor<TTestCase, TDelta, TModel>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TDelta : IDelta<TModel>
	{
		private readonly ITestsExecutor<TTestCase, TDelta, TModel> executor;
		private readonly ITestsInstrumentor<TModel, TTestCase> instrumentor;
		private readonly IDataStructureProvider<CorrespondenceModel.Models.CorrespondenceModel, TModel> dataStructureProvider;
		private readonly IApplicationClosedHandler applicationClosedHandler;

		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;

		public TestsExecutorWithInstrumenting(ITestsExecutor<TTestCase, TDelta, TModel> executor,
			ITestsInstrumentor<TModel, TTestCase> instrumentor,
			IDataStructureProvider<CorrespondenceModel.Models.CorrespondenceModel, TModel> dataStructureProvider,
			IApplicationClosedHandler applicationClosedHandler)
		{
			this.executor = executor;
			this.instrumentor = instrumentor;
			this.dataStructureProvider = dataStructureProvider;
			this.applicationClosedHandler = applicationClosedHandler;
		}

		public async Task<ITestsExecutionResult<TTestCase>> ProcessTests(IList<TTestCase> impactedTests, IList<TTestCase> allTests, TDelta impactedForDelta,
			CancellationToken cancellationToken)
		{
			using (instrumentor)
			{
				applicationClosedHandler.AddApplicationClosedListener(instrumentor);

				await instrumentor.InstrumentModelForTests(impactedForDelta.TargetModel, impactedTests, cancellationToken);

				executor.TestResultAvailable += TestResultAvailable;
				var result = await executor.ProcessTests(impactedTests, allTests, impactedForDelta, cancellationToken);
				executor.TestResultAvailable -= TestResultAvailable;

				var coverage = instrumentor.GetCoverageData();

				await UpdateCorrespondenceModel(coverage, impactedForDelta, allTests, cancellationToken);

				applicationClosedHandler.RemovedApplicationClosedListener(instrumentor);
				return result;
			}
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
				/* TODO Granularity Level File
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