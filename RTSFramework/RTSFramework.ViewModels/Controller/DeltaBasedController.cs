using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.RTSApproach;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Dynamic;

namespace RTSFramework.ViewModels.Controller
{
	public class DeltaBasedController<TModel, TDelta, TTestCase, TResult>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TDelta : IDelta<TModel>
		where TResult : ITestProcessingResult
	{
		private readonly ITestProcessor<TTestCase, TResult> testProcessor;
		private readonly ITestsDiscoverer<TModel, TTestCase> testsDiscoverer;
		private readonly IRTSApproach<TModel, TDelta, TTestCase> rtsApproach;

		public DeltaBasedController(
			ITestsDiscoverer<TModel, TTestCase> testsDiscoverer,
			IRTSApproach<TModel, TDelta, TTestCase> rtsApproach,
			ITestProcessor<TTestCase, TResult> testProcessor)
		{
			this.testProcessor = testProcessor;
			this.testsDiscoverer = testsDiscoverer;
			this.rtsApproach = rtsApproach;
		}

		public async Task<TResult> ExecuteImpactedTests(TDelta delta, CancellationToken token)
		{
			var allTests = await DebugStopWatchTracker.ReportNeededTimeOnDebug(testsDiscoverer.GetTestCasesForModel(delta.TargetModel, token), "TestsDiscovery");
			if (token.IsCancellationRequested)
			{
				return default(TResult);
			}

			var impactedTests = new List<TTestCase>();
			rtsApproach.ImpactedTest += (sender, args) =>
			{
				var impactedTest = args.TestCase;

				Debug.WriteLine($"Impacted Test: {impactedTest.Id}");
				impactedTests.Add(impactedTest);
			};

			DebugStopWatchTracker.ReportNeededTimeOnDebug(() => rtsApproach.ExecuteRTS(allTests, delta, token), "RTSApproach");

			Debug.WriteLine($"{impactedTests.Count} Tests impacted");

			if (token.IsCancellationRequested)
			{
				return default(TResult);
			}

			var processingResult = await DebugStopWatchTracker.ReportNeededTimeOnDebug(testProcessor.ProcessTests(impactedTests, token), "ProcessingOfImpactedTests");
			if (token.IsCancellationRequested)
			{
				return default(TResult);
			}

			var testExecutionResult = processingResult as MSTestExectionResult;
			if (testExecutionResult != null && testExecutionResult.CoverageData != null)
			{
				var dynamicRtsApproach = rtsApproach as IDynamicRTSApproach;
				dynamicRtsApproach?.UpdateCorrespondenceModel(testExecutionResult.CoverageData);
			}

			return processingResult;
		}
	}
}