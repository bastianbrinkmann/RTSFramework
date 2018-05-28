using System;
using System.Collections.Generic;
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

namespace RTSFramework.ViewModels.Controller
{
	public class DeltaBasedController<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TParsedDelta : IDelta<TModel>
		where TSelectionDelta : IDelta<TModel>
		where TResult : ITestProcessingResult
	{
		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
		public event EventHandler<TestsPrioritizedEventArgs<TTestCase>> TestsPrioritized;

		private readonly IArtefactAdapter<TDeltaArtefact, TParsedDelta> deltaArtefactAdapter;
		private readonly ModelLevelController<TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult> modelLevelController;
		private readonly ILoggingHelper loggingHelper;
		private readonly IArtefactAdapter<TResultArtefact, TResult> resultArtefactAdapter;

		public DeltaBasedController(
			IArtefactAdapter<TDeltaArtefact, TParsedDelta> deltaArtefactAdapter,
			ModelLevelController<TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult> modelLevelController,
			IArtefactAdapter<TResultArtefact, TResult> resultArtefactAdapter,
			ILoggingHelper loggingHelper)
		{
			this.deltaArtefactAdapter = deltaArtefactAdapter;
			this.modelLevelController = modelLevelController;
			this.resultArtefactAdapter = resultArtefactAdapter;
			this.loggingHelper = loggingHelper;
		}

		public Func<TTestCase, bool> FilterFunction { private get; set; }

		public async Task<TResultArtefact> ExecuteRTSRun(TDeltaArtefact deltaArtefact, CancellationToken token)
		{
			loggingHelper.InitLogFile();

			var parsedDelta = deltaArtefactAdapter.Parse(deltaArtefact);
			token.ThrowIfCancellationRequested();

			modelLevelController.TestResultAvailable += TestResultAvailable;
			modelLevelController.TestsPrioritized += TestsPrioritized;
			modelLevelController.ImpactedTest += ImpactedTest;

			modelLevelController.FilterFunction = FilterFunction;

			var processingResult = await modelLevelController.ExecuteRTSRun(parsedDelta, token);

			modelLevelController.TestResultAvailable -= TestResultAvailable;
			modelLevelController.TestsPrioritized -= TestsPrioritized;
			modelLevelController.ImpactedTest -= ImpactedTest;

			return resultArtefactAdapter.Unparse(processingResult);
		}
	}
}