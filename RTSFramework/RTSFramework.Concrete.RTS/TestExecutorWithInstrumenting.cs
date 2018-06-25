using System;
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
using RTSFramework.RTSApproaches.CorrespondenceModel;
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
		private readonly CorrespondenceModelManager<TModel> correspondenceModelManager;
		private readonly IApplicationClosedHandler applicationClosedHandler;
		private readonly ILoggingHelper loggingHelper;

		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;

		public TestExecutorWithInstrumenting(ITestExecutor<TTestCase, TDelta, TModel> executor,
			ITestsInstrumentor<TModel, TTestCase> instrumentor,
			CorrespondenceModelManager<TModel> correspondenceModelManager,
			IApplicationClosedHandler applicationClosedHandler,
			ILoggingHelper loggingHelper)
		{
			this.executor = executor;
			this.instrumentor = instrumentor;
			this.correspondenceModelManager = correspondenceModelManager;
			this.applicationClosedHandler = applicationClosedHandler;
			this.loggingHelper = loggingHelper;
		}

		public async Task<ITestsExecutionResult<TTestCase>> ProcessTests(IList<TTestCase> impactedTests, StructuralDelta<ISet<TTestCase>, TTestCase> testsDelta, TDelta impactedForDelta,
			CancellationToken cancellationToken)
		{
			using (instrumentor)
			{
				applicationClosedHandler.AddApplicationClosedListener(instrumentor);

				await instrumentor.Instrument(impactedForDelta.NewModel, impactedTests, cancellationToken);

				executor.TestResultAvailable += TestResultAvailable;
				var result = await executor.ProcessTests(impactedTests, testsDelta, impactedForDelta, cancellationToken);
				executor.TestResultAvailable -= TestResultAvailable;

				CorrespondenceLinks coverage = instrumentor.GetCorrespondenceLinks();

				var failedTests = result.TestcasesResults.Where(x => x.Outcome == TestExecutionOutcome.Failed).Select(x => x.TestCase.Id).ToList();

				var coveredTests = coverage.Links.Select(x => x.Item1).Distinct().ToList();
				var testsWithoutCoverage = impactedTests.Where(x => !coveredTests.Contains(x.Id)).Select(x => x.Id).ToList();
				
				testsWithoutCoverage.ForEach(x => loggingHelper.WriteMessage("Not covered: " + x));
				failedTests.ForEach(x => loggingHelper.WriteMessage("Failed Tests: " + x));

				testsWithoutCoverage.Except(failedTests).ForEach(x => loggingHelper.WriteMessage("Not covered and not failed Tests: " + x));

				correspondenceModelManager.UpdateCorrespondenceModel(coverage, impactedForDelta, testsDelta.DeletedElements.Select(x => x.Id), failedTests);

				applicationClosedHandler.RemovedApplicationClosedListener(instrumentor);
				return result;
			}
		}
	}
}