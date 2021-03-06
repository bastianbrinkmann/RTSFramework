﻿using System;
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
	public class DeltaBasedController<TDeltaArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, TVisualizationArtefact>
		: IArtefactBasedController<TVisualizationArtefact>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TProgramDelta : IDelta<TModel>
		where TResult : ITestProcessingResult
	{
		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
		public event EventHandler<TestsPrioritizedEventArgs<TTestCase>> TestsPrioritized;

		private readonly IArtefactAdapter<TDeltaArtefact, TProgramDelta> deltaArtefactAdapter;
		private readonly ModelBasedController<TModel, TProgramDelta, TTestCase, TResult> modelBasedController;
		private readonly ILoggingHelper loggingHelper;
		private readonly IArtefactAdapter<TResultArtefact, TResult> resultArtefactAdapter;
		private readonly Lazy<IArtefactAdapter<TVisualizationArtefact, VisualizationData>> visualizationArtefactAdapter;

		public DeltaBasedController(
			IArtefactAdapter<TDeltaArtefact, TProgramDelta> deltaArtefactAdapter,
			ModelBasedController<TModel, TProgramDelta, TTestCase, TResult> modelBasedController,
			IArtefactAdapter<TResultArtefact, TResult> resultArtefactAdapter,
			ILoggingHelper loggingHelper,
			Lazy<IArtefactAdapter<TVisualizationArtefact, VisualizationData>> visualizationArtefactAdapter)
		{
			this.deltaArtefactAdapter = deltaArtefactAdapter;
			this.modelBasedController = modelBasedController;
			this.resultArtefactAdapter = resultArtefactAdapter;
			this.loggingHelper = loggingHelper;
			this.visualizationArtefactAdapter = visualizationArtefactAdapter;
		}

		public Func<TTestCase, bool> FilterFunction { private get; set; }

		public async Task<TResultArtefact> ExecuteRTSRun(TDeltaArtefact deltaArtefact, CancellationToken token)
		{
			loggingHelper.InitLogFile();

			var parsedDelta = deltaArtefactAdapter.Parse(deltaArtefact);
			token.ThrowIfCancellationRequested();

			modelBasedController.TestResultAvailable += TestResultAvailable;
			modelBasedController.TestsPrioritized += TestsPrioritized;
			modelBasedController.ImpactedTest += ImpactedTest;

			modelBasedController.FilterFunction = FilterFunction;

			var processingResult = await modelBasedController.ExecuteRTSRun(parsedDelta, token);

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