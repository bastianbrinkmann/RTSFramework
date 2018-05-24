using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;

namespace RTSFramework.Core
{
	public abstract class TestExecutorWithTerminationCondition<TTestCase, TDelta, TModel> : ITestExecutor<TTestCase, TDelta, TModel> 
		where TTestCase : ITestCase 
		where TDelta : IDelta<TModel> 
		where TModel : IProgramModel
	{
		private readonly ITestExecutor<TTestCase, TDelta, TModel> internalExecutor;

		/// <summary>
		/// Checks every second whether the termination condition is reached and stops processing immediatly once the state is reached
		/// </summary>
		/// <param name="internalExecutor"></param>
		public TestExecutorWithTerminationCondition(ITestExecutor<TTestCase, TDelta, TModel> internalExecutor)
		{
			this.internalExecutor = internalExecutor;
		}

		public event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;

		public abstract bool IsTerminationConditionFulfilled();

		public virtual async Task<ITestsExecutionResult<TTestCase>> ProcessTests(IList<TTestCase> impactedTests, ISet<TTestCase> allTests, TDelta impactedForDelta,
			CancellationToken cancellationToken)
		{
			var tokenSource = new CancellationTokenSource();
			var terminationConditionToken = tokenSource.Token;

			cancellationToken.Register(tokenSource.Cancel);

			internalExecutor.TestResultAvailable += TestResultAvailable;
			
			Task<ITestsExecutionResult<TTestCase>> processingTask = Task.Run(() => internalExecutor.ProcessTests(impactedTests, allTests, impactedForDelta, terminationConditionToken), terminationConditionToken);

			while (!IsTerminationConditionFulfilled() && !processingTask.IsCompleted)
			{
				await Task.Delay(100, terminationConditionToken);
			}

			if (IsTerminationConditionFulfilled())
			{
				tokenSource.Cancel();
				throw new TerminationConditionReachedException();
			}

			internalExecutor.TestResultAvailable -= TestResultAvailable;

			return processingTask.Result;
		}
	}
}