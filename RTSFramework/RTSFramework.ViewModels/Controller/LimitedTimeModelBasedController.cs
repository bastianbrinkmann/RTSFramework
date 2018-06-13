using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.SecondaryFeature;
using RTSFramework.Contracts.Utilities;
using RTSFramework.Core;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.ViewModels.Controller
{
	public class LimitedTimeModelBasedController<TModel, TInputDelta, TSelectionDelta, TTestCase, TResult> 
		: ModelBasedController<TModel, TInputDelta, TSelectionDelta, TTestCase, TResult>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TInputDelta : IDelta<TModel>
		where TSelectionDelta : IDelta<TModel>
		where TResult : ITestProcessingResult
	{
		private readonly IUserRunConfigurationProvider runConfigurationProvider;

		public LimitedTimeModelBasedController(IDeltaAdapter<TInputDelta, TSelectionDelta, TModel> deltaAdapter, 
			Lazy<IOfflineDeltaDiscoverer<TModel, TInputDelta>> deltaDiscoverer, 
			ITestDiscoverer<TModel, TSelectionDelta, TTestCase> testDiscoverer, 
			ITestSelector<TModel, TSelectionDelta, TTestCase> testSelector,
			ITestProcessor<TTestCase, TResult, TSelectionDelta, TModel> testProcessor,
			ITestPrioritizer<TTestCase> testPrioritizer, ILoggingHelper loggingHelper,
			IUserRunConfigurationProvider runConfigurationProvider,
			Lazy<IDependenciesVisualizer> dependenciesVisualizer)
			: base(deltaAdapter, deltaDiscoverer, testDiscoverer, testSelector, testProcessor, testPrioritizer, loggingHelper, dependenciesVisualizer)
		{
			this.runConfigurationProvider = runConfigurationProvider;
		}

		public override async Task<TResult> ExecuteRTSRun(TInputDelta delta, CancellationToken token)
		{
			double timeLimitInSeconds = runConfigurationProvider.TimeLimit;

			var tokenSource = new CancellationTokenSource();
			var timeLimitToken = tokenSource.Token;
			token.Register(tokenSource.Cancel);

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			Task<TResult> task = Task.Run(() => base.ExecuteRTSRun(delta, timeLimitToken), timeLimitToken);

			while (stopwatch.Elapsed.TotalSeconds <= timeLimitInSeconds && !task.IsCompleted)
			{
				await Task.Delay(100, timeLimitToken);
			}

			if (stopwatch.Elapsed.TotalSeconds > timeLimitInSeconds)
			{
				tokenSource.Cancel();
				throw new TerminationConditionReachedException();
			}

			return task.Result;
		}
	}
}