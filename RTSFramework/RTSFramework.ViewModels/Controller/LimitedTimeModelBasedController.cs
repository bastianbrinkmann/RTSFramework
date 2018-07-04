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
	public class LimitedTimeModelBasedController<TModel, TProgramDelta, TTestCase, TResult> 
		: ModelBasedController<TModel, TProgramDelta, TTestCase, TResult>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TProgramDelta : IDelta<TModel>
		where TResult : ITestProcessingResult
	{
		private readonly IUserRunConfigurationProvider runConfigurationProvider;

		public LimitedTimeModelBasedController(Lazy<IOfflineDeltaDiscoverer<TModel, TProgramDelta>> deltaDiscoverer, 
			ITestDiscoverer<TModel, TProgramDelta, TTestCase> testDiscoverer, 
			ITestSelector<TModel, TProgramDelta, TTestCase> testSelector,
			ITestProcessor<TTestCase, TResult, TProgramDelta, TModel> testProcessor,
			ITestPrioritizer<TTestCase> testPrioritizer, ILoggingHelper loggingHelper,
			IUserRunConfigurationProvider runConfigurationProvider,
			Lazy<IDependenciesVisualizer> dependenciesVisualizer,
			IResponsibleChangesReporter<TTestCase, TModel, TProgramDelta> responsibleChangesReporter)
			: base(deltaDiscoverer, testDiscoverer, testSelector, testProcessor, testPrioritizer, loggingHelper, dependenciesVisualizer, responsibleChangesReporter)
		{
			this.runConfigurationProvider = runConfigurationProvider;
		}

		public override async Task<TResult> ExecuteRTSRun(TProgramDelta delta, CancellationToken token)
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