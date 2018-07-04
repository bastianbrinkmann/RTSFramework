using System;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;

namespace RTSFramework.Contracts
{
	public interface ITestExecutor<TTestCase, TDelta, TProgram> : ITestProcessor<TTestCase, ITestsExecutionResult<TTestCase>, TDelta, TProgram>
		where TTestCase : ITestCase
		where TDelta : IDelta<TProgram>
		where TProgram : IProgramModel
	{
		event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
	}
}