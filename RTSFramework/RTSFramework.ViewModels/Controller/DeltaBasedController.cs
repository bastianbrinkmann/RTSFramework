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
	public class DeltaBasedController<TDeltaArtefact, TModel, TDelta, TTestCase, TResult, TResultArtefact>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TDelta : IDelta<TModel>
		where TResult : ITestProcessingResult
	{
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
		public event EventHandler<TestsPrioritizedEventArgs<TTestCase>> TestsPrioritized;

		private readonly IArtefactAdapter<TDeltaArtefact, TDelta> deltaArtefactAdapter;
		private readonly ILoggingHelper loggingHelper;
		private readonly IArtefactAdapter<TResultArtefact, TResult> resultArtefactAdapter;
		private readonly IModelBasedController<TModel, TDelta, TTestCase, TResult> modelBasedController;

		public DeltaBasedController(
			IArtefactAdapter<TDeltaArtefact, TDelta> deltaArtefactAdapter,
			ILoggingHelper loggingHelper,
			IArtefactAdapter<TResultArtefact, TResult> resultArtefactAdapter,
			IModelBasedController<TModel, TDelta, TTestCase, TResult> modelBasedController)
		{
			this.deltaArtefactAdapter = deltaArtefactAdapter;
			this.loggingHelper = loggingHelper;
			this.resultArtefactAdapter = resultArtefactAdapter;
			this.modelBasedController = modelBasedController;
		}

		public Func<TTestCase, bool> FilterFunction { private get; set; }

		public async Task<TResultArtefact> ExecuteRTSRun(TDeltaArtefact deltaArtefact, CancellationToken token)
		{
			loggingHelper.InitLogFile();

			var parsedDelta = deltaArtefactAdapter.Parse(deltaArtefact);
			token.ThrowIfCancellationRequested();

			modelBasedController.ImpactedTest += ImpactedTest;
			modelBasedController.TestResultAvailable += TestResultAvailable;
			modelBasedController.TestsPrioritized += TestsPrioritized;

			modelBasedController.FilterFunction = FilterFunction;

			var result = await modelBasedController.ExecuteRTSRun(parsedDelta, token);

			modelBasedController.ImpactedTest -= ImpactedTest;
			modelBasedController.TestResultAvailable -= TestResultAvailable;
			modelBasedController.TestsPrioritized -= TestsPrioritized;

			return resultArtefactAdapter.Unparse(result);
		}
	}
}