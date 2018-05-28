using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.Contracts.Utilities;
using RTSFramework.RTSApproaches.Core;
using RTSFramework.RTSApproaches.Core.Contracts;
using RTSFramework.RTSApproaches.CorrespondenceModel;
using RTSFramework.RTSApproaches.CorrespondenceModel.Models;
using RTSFramework.RTSApproaches.Dynamic;
using Unity.Interception.Utilities;

namespace RTSFramework.ViewModels.Controller
{
	public class CorrespondenceModelCreationController<TModel, TInputDelta, TSelectionDelta, TTestCase, TResult> : IModelBasedController<TModel, TInputDelta, TTestCase, TResult>
		where TTestCase : class, ITestCase
		where TModel : IProgramModel
		where TInputDelta : IDelta<TModel>
		where TSelectionDelta : IDelta<TModel>
		where TResult : class, ITestProcessingResult 
	{
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
		public event EventHandler<TestsPrioritizedEventArgs<TTestCase>> TestsPrioritized;

		private readonly IOfflineDeltaDiscoverer<TModel, TInputDelta> deltaDiscoverer;
		private readonly IDeltaAdapter<TInputDelta, TSelectionDelta, TModel> deltaAdapter;
		private readonly ITestDiscoverer<TModel, TTestCase> testDiscoverer;
		private readonly CorrespondenceModelManager<TModel> correspondenceModelManager;
		private readonly ITestSelector<TModel, TSelectionDelta, TTestCase> testSelector;
		private readonly Lazy<ITestsInstrumentor<TModel, TTestCase>> instrumentor;
		private readonly ITestExecutor<TTestCase, TSelectionDelta, TModel> testExecutor;
		private readonly ITestPrioritizer<TTestCase> testPrioritizer;
		private readonly ILoggingHelper loggingHelper;
		private readonly IApplicationClosedHandler applicationClosedHandler;

		public CorrespondenceModelCreationController(
			IOfflineDeltaDiscoverer<TModel, TInputDelta> deltaDiscoverer,
			IDeltaAdapter<TInputDelta, TSelectionDelta, TModel> deltaAdapter,
			ITestDiscoverer<TModel, TTestCase> testDiscoverer,
			CorrespondenceModelManager<TModel> correspondenceModelManager,
			ITestSelector<TModel, TSelectionDelta, TTestCase> testSelector,
			Lazy<ITestsInstrumentor<TModel, TTestCase>> instrumentor,
			ITestExecutor<TTestCase, TSelectionDelta, TModel> testExecutor,
			ITestPrioritizer<TTestCase> testPrioritizer,
			ILoggingHelper loggingHelper,
			IApplicationClosedHandler applicationClosedHandler)
		{
			this.deltaDiscoverer = deltaDiscoverer;
			this.deltaAdapter = deltaAdapter;
			this.testDiscoverer = testDiscoverer;
			this.correspondenceModelManager = correspondenceModelManager;
			this.testSelector = testSelector;
			this.instrumentor = instrumentor;
			this.testExecutor = testExecutor;
			this.testPrioritizer = testPrioritizer;
			this.loggingHelper = loggingHelper;
			this.applicationClosedHandler = applicationClosedHandler;
		}

		public Func<TTestCase, bool> FilterFunction { private get; set; }

		public async Task<TResult> ExecuteRTSRun(TModel oldProgramModel, TModel newProgramModel, CancellationToken token)
		{
			var delta = loggingHelper.ReportNeededTime(() => deltaDiscoverer.Discover(oldProgramModel, newProgramModel), "Delta Discovery");
			token.ThrowIfCancellationRequested();

			return await ExecuteRTSRun(delta, token);
		}

		public async Task<TResult> ExecuteRTSRun(TInputDelta inputDelta, CancellationToken token)
		{
			var convertedDelta = deltaAdapter.Convert(inputDelta);

			var allTests = await loggingHelper.ReportNeededTime(() => testDiscoverer.GetTests(inputDelta.NewModel, FilterFunction, token), "Tests Discovery");
			token.ThrowIfCancellationRequested();

			await loggingHelper.ReportNeededTime(() => testSelector.SelectTests(allTests, convertedDelta, token), "Test Selection");
			var impactedTests = testSelector.SelectedTests;

			foreach (var impactedTest in impactedTests)
			{
				ImpactedTest?.Invoke(this, new ImpactedTestEventArgs<TTestCase>(impactedTest, testSelector.GetResponsibleChangesByTestId?.Invoke(impactedTest.Id)));
			}

			loggingHelper.WriteMessage($"{impactedTests.Count} Tests impacted");

			var prioritizedTests = await loggingHelper.ReportNeededTime(() => testPrioritizer.PrioritizeTests(impactedTests, token), "Tests Prioritization");

			TestsPrioritized?.Invoke(this, new TestsPrioritizedEventArgs<TTestCase>(prioritizedTests));

			var processingResult = await ExecuteTestsWithInstrumenting(convertedDelta, prioritizedTests, allTests, token);

			return processingResult;
		}

		private async Task<TResult> ExecuteTestsWithInstrumenting(TSelectionDelta convertedDelta, IList<TTestCase> prioritizedTests, ISet<TTestCase> allTests, CancellationToken token)
		{
			var instrumentorInstance = instrumentor.Value;

			using (instrumentorInstance)
			{
				applicationClosedHandler.AddApplicationClosedListener(instrumentorInstance);

				await instrumentorInstance.InstrumentModelForTests(convertedDelta.NewModel, prioritizedTests, token);

				testExecutor.TestResultAvailable += TestResultAvailable;
				var result = await testExecutor.ProcessTests(prioritizedTests, allTests, convertedDelta, token);
				testExecutor.TestResultAvailable -= TestResultAvailable;

				var coverage = instrumentorInstance.GetCoverageData();

				var failedTests = result.TestcasesResults.Where(x => x.Outcome == TestExecutionOutcome.Failed).Select(x => x.TestCase.Id).ToList();

				var coveredTests = coverage.CoverageDataEntries.Select(x => x.Item1).Distinct().ToList();
				var testsWithoutCoverage = prioritizedTests.Where(x => !coveredTests.Contains(x.Id)).Select(x => x.Id).ToList();

				testsWithoutCoverage.ForEach(x => loggingHelper.WriteMessage("Not covered: " + x));
				failedTests.ForEach(x => loggingHelper.WriteMessage("Failed Tests: " + x));

				testsWithoutCoverage.Except(failedTests).ForEach(x => loggingHelper.WriteMessage("Not covered and not failed Tests: " + x));

				var correspondeceModel = await loggingHelper.ReportNeededTime(() => correspondenceModelManager.GetCorrespondenceModel(convertedDelta.OldModel), "Getting old CorrespondenceModel");

				await UpdateCorrespondenceModel(correspondeceModel, coverage, convertedDelta, allTests, failedTests);

				applicationClosedHandler.RemovedApplicationClosedListener(instrumentorInstance);

				return result as TResult;
			}
		}

		private async Task UpdateCorrespondenceModel(CorrespondenceModel oldCorrespondenceModel, CoverageData coverageData, TSelectionDelta currentDelta, ISet<TTestCase> allTests, IList<string> failedTests)
		{
			var newModel = oldCorrespondenceModel.CloneModel(currentDelta.NewModel.VersionId);
			newModel.UpdateByNewLinks(GetLinksByCoverageData(coverageData, currentDelta.NewModel));
			newModel.RemoveDeletedTests(allTests.Select(x => x.Id));

			failedTests.ForEach(x => newModel.CorrespondenceModelLinks.Remove(x));

			await correspondenceModelManager.PersistCorrespondenceModel(newModel);
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