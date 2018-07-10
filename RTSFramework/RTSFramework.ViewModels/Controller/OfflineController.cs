using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.Contracts.Utilities;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Core;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.ViewModels.Controller
{
	public class OfflineController<TArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, TVisualizationArtefact> 
		: IArtefactBasedController<TVisualizationArtefact>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TProgramDelta : IDelta<TModel>
		where TResult : ITestProcessingResult
	{
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
		public event EventHandler<TestsPrioritizedEventArgs<TTestCase>> TestsPrioritized;

		private readonly IArtefactAdapter<TArtefact, TModel> artefactAdapter;
		private readonly ModelBasedController<TModel, TProgramDelta, TTestCase, TResult> modelBasedController;
		private readonly ILoggingHelper loggingHelper;
		private readonly IArtefactAdapter<TResultArtefact, TResult> resultArtefactAdapter;
		private readonly Lazy<IArtefactAdapter<TVisualizationArtefact, VisualizationData>> visualizationArtefactAdapter;

		public OfflineController(
			IArtefactAdapter<TArtefact, TModel> artefactAdapter,
			ModelBasedController<TModel, TProgramDelta, TTestCase, TResult> modelBasedController,
			ILoggingHelper loggingHelper,
			IArtefactAdapter<TResultArtefact, TResult> resultArtefactAdapter,
			Lazy<IArtefactAdapter<TVisualizationArtefact, VisualizationData>> visualizationArtefactAdapter)
		{
			this.artefactAdapter = artefactAdapter;
			this.modelBasedController = modelBasedController;
			this.loggingHelper = loggingHelper;
			this.resultArtefactAdapter = resultArtefactAdapter;
			this.visualizationArtefactAdapter = visualizationArtefactAdapter;
		}

		public Func<TTestCase, bool> FilterFunction { private get; set; }

		public async Task<TResultArtefact> ExecuteRTSRun(TArtefact oldArtefact, TArtefact newArtefact, CancellationToken token)
		{
			loggingHelper.InitLogFile();

			var oldModel = artefactAdapter.Parse(oldArtefact);
			var newModel = artefactAdapter.Parse(newArtefact);

			modelBasedController.TestResultAvailable += TestResultAvailable;
			modelBasedController.TestsPrioritized += TestsPrioritized;
			modelBasedController.ImpactedTest += ImpactedTest;

			modelBasedController.FilterFunction = FilterFunction;

			var processingResult = await modelBasedController.ExecuteRTSRun(oldModel, newModel, token);

			modelBasedController.TestResultAvailable -= TestResultAvailable;
			modelBasedController.TestsPrioritized -= TestsPrioritized;
			modelBasedController.ImpactedTest -= ImpactedTest;

			return resultArtefactAdapter.Unparse(processingResult);
		}

		public TVisualizationArtefact GetDependenciesVisualization()
		{
			return visualizationArtefactAdapter.Value.Unparse(modelBasedController.GetDependenciesVisualization());
		}
	}
}