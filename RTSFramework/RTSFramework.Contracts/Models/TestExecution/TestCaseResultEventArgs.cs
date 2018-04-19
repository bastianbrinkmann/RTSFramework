using System;

namespace RTSFramework.Contracts.Models.TestExecution
{
	public class TestCaseResultEventArgs<TTestCase> : EventArgs where TTestCase : ITestCase
	{
		public TestCaseResultEventArgs(ITestCaseResult<TTestCase> testResult)
		{
			TestResult = testResult;
		}

		public ITestCaseResult<TTestCase> TestResult { get; private set; }
	}
}