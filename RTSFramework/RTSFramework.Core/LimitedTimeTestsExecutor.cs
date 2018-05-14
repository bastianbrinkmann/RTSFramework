using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.Core
{
	public class LimitedTimeTestsExecutor<TTestCase, TDelta, TModel> : TestsExecutorWithTerminationCondition<TTestCase, TDelta, TModel>
		where TTestCase : ITestCase
		where TDelta : IDelta<TModel>
		where TModel : IProgramModel
	{
		private readonly IUserRunConfigurationProvider userRunConfigurationProvider;

		public LimitedTimeTestsExecutor(ITestsExecutor<TTestCase, TDelta, TModel> executor,
			IUserRunConfigurationProvider userRunConfigurationProvider) : base(executor)
		{
			this.userRunConfigurationProvider = userRunConfigurationProvider;
		}

		private Stopwatch stopwatch;
		private double timeLimitInSeconds;

		public override bool IsTerminationConditionFulfilled()
		{
			return stopwatch.Elapsed.TotalSeconds > timeLimitInSeconds;
		}

		public override async Task<ITestsExecutionResult<TTestCase>> ProcessTests(IList<TTestCase> impactedTests, IList<TTestCase> allTests, TDelta impactedForDelta, CancellationToken cancellationToken)
		{
			timeLimitInSeconds = userRunConfigurationProvider.TimeLimit;

			stopwatch = new Stopwatch();
			stopwatch.Start();
			var result = await base.ProcessTests(impactedTests, allTests, impactedForDelta, cancellationToken);
			stopwatch.Stop();
			return result;
		}
	}
}