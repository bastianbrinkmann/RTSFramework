using System;

namespace RTSFramework.Contracts.Models.TestExecution
{
	public interface ITestCaseResult<TTestCase> where TTestCase : ITestCase
	{
		TTestCase TestCase { get; }
		string ErrorMessage { get; }
		string StackTrace { get; }
		double DurationInSeconds { get; }
		DateTimeOffset StartTime { get; }
		DateTimeOffset EndTime { get; }
		TestExecutionOutcome Outcome { get; }
    }
}