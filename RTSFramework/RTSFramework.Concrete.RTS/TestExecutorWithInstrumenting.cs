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
using Unity.Interception.Utilities;

namespace RTSFramework.RTSApproaches.Dynamic
{
	public class TestExecutorWithInstrumenting<TModel, TDelta, TTestCase> : ITestExecutor<TTestCase, TDelta, TModel>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TDelta : IDelta<TModel>
	{
		private readonly ITestExecutor<TTestCase, TDelta, TModel> executor;
		private readonly ITestsInstrumentor<TModel, TTestCase> instrumentor;
		private readonly IDataStructureProvider<CorrespondenceModel.Models.CorrespondenceModel, TModel> dataStructureProvider;
		private readonly IApplicationClosedHandler applicationClosedHandler;
		private readonly ILoggingHelper loggingHelper;

		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;

		public TestExecutorWithInstrumenting(ITestExecutor<TTestCase, TDelta, TModel> executor,
			ITestsInstrumentor<TModel, TTestCase> instrumentor,
			IDataStructureProvider<CorrespondenceModel.Models.CorrespondenceModel, TModel> dataStructureProvider,
			IApplicationClosedHandler applicationClosedHandler,
			ILoggingHelper loggingHelper)
		{
			this.executor = executor;
			this.instrumentor = instrumentor;
			this.dataStructureProvider = dataStructureProvider;
			this.applicationClosedHandler = applicationClosedHandler;
			this.loggingHelper = loggingHelper;
		}

		public async Task<ITestsExecutionResult<TTestCase>> ProcessTests(IList<TTestCase> impactedTests, ISet<TTestCase> allTests, TDelta impactedForDelta,
			CancellationToken cancellationToken)
		{
			using (instrumentor)
			{
				applicationClosedHandler.AddApplicationClosedListener(instrumentor);

				await instrumentor.InstrumentModelForTests(impactedForDelta.NewModel, impactedTests, cancellationToken);

				executor.TestResultAvailable += TestResultAvailable;
				var result = await executor.ProcessTests(impactedTests, allTests, impactedForDelta, cancellationToken);
				executor.TestResultAvailable -= TestResultAvailable;

				var coverage = instrumentor.GetCoverageData();

				var failedTests = result.TestcasesResults.Where(x => x.Outcome == TestExecutionOutcome.Failed).Select(x => x.TestCase.Id).ToList();

				var coveredTests = coverage.CoverageDataEntries.Select(x => x.Item1).Distinct().ToList();
				var testsWithoutCoverage = impactedTests.Where(x => !coveredTests.Contains(x.Id)).Select(x => x.Id).ToList();
				
				testsWithoutCoverage.ForEach(x => loggingHelper.WriteMessage("Not covered: " + x));
				failedTests.ForEach(x => loggingHelper.WriteMessage("Failed Tests: " + x));

				testsWithoutCoverage.Except(failedTests).ForEach(x => loggingHelper.WriteMessage("Not covered and not failed Tests: " + x));

				await UpdateCorrespondenceModel(coverage, impactedForDelta, allTests, failedTests, cancellationToken);

				applicationClosedHandler.RemovedApplicationClosedListener(instrumentor);
				return result;
			}
		}

		private async Task UpdateCorrespondenceModel(CoverageData coverageData, TDelta currentDelta, ISet<TTestCase> allTests, IList<string> failedTests, CancellationToken token)
		{
			var oldModel = await dataStructureProvider.GetDataStructureForProgram(currentDelta.OldModel, token);
			var newModel = oldModel.CloneModel(currentDelta.NewModel.VersionId);
			newModel.UpdateByNewLinks(GetLinksByCoverageData(coverageData, currentDelta.NewModel));
			newModel.RemoveDeletedTests(allTests.Select(x => x.Id));

			failedTests.ForEach(x => newModel.CorrespondenceModelLinks.Remove(x));

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