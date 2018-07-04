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
using RTSFramework.RTSApproaches.CorrespondenceModel;
using Unity.Interception.Utilities;

namespace RTSFramework.RTSApproaches.Dynamic
{
	public class TestExecutorWithInstrumenting<TProgram, TProgramDelta, TTestCase> : ITestExecutor<TTestCase, TProgramDelta, TProgram>
		where TTestCase : ITestCase
		where TProgram : IProgramModel
		where TProgramDelta : IDelta<TProgram>
	{
		private readonly ITestExecutor<TTestCase, TProgramDelta, TProgram> executor;
		private readonly ITestsInstrumentor<TProgram, TTestCase> instrumentor;
		private readonly CorrespondenceModelManager<TProgram> correspondenceModelManager;
		private readonly IApplicationClosedHandler applicationClosedHandler;
		private readonly ILoggingHelper loggingHelper;

		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;

		public TestExecutorWithInstrumenting(ITestExecutor<TTestCase, TProgramDelta, TProgram> executor,
			ITestsInstrumentor<TProgram, TTestCase> instrumentor,
			CorrespondenceModelManager<TProgram> correspondenceModelManager,
			IApplicationClosedHandler applicationClosedHandler,
			ILoggingHelper loggingHelper)
		{
			this.executor = executor;
			this.instrumentor = instrumentor;
			this.correspondenceModelManager = correspondenceModelManager;
			this.applicationClosedHandler = applicationClosedHandler;
			this.loggingHelper = loggingHelper;
		}

		public async Task<ITestsExecutionResult<TTestCase>> ProcessTests(IList<TTestCase> impactedTests, StructuralDelta<TestsModel<TTestCase>, TTestCase> testsDelta, TProgramDelta programDelta,
			CancellationToken cancellationToken)
		{
			using (instrumentor)
			{
				applicationClosedHandler.AddApplicationClosedListener(instrumentor);

				await instrumentor.Instrument(programDelta.NewModel, impactedTests, cancellationToken);

				executor.TestResultAvailable += TestResultAvailable;
				var result = await executor.ProcessTests(impactedTests, testsDelta, programDelta, cancellationToken);
				executor.TestResultAvailable -= TestResultAvailable;

				CorrespondenceLinks coverage = instrumentor.GetCorrespondenceLinks();

				var failedTests = result.TestcasesResults.Where(x => x.Outcome == TestExecutionOutcome.Failed).Select(x => x.TestCase.Id).ToList();

				var coveredTests = coverage.Links.Select(x => x.Item1).Distinct().ToList();
				var testsWithoutCoverage = impactedTests.Where(x => !coveredTests.Contains(x.Id)).Select(x => x.Id).ToList();
				
				testsWithoutCoverage.ForEach(x => loggingHelper.WriteMessage("Not covered: " + x));
				failedTests.ForEach(x => loggingHelper.WriteMessage("Failed Tests: " + x));

				testsWithoutCoverage.Except(failedTests).ForEach(x => loggingHelper.WriteMessage("Not covered and not failed Tests: " + x));

				correspondenceModelManager.UpdateCorrespondenceModel(coverage, programDelta, testsDelta, failedTests);

				applicationClosedHandler.RemovedApplicationClosedListener(instrumentor);
				return result;
			}
		}
	}
}