using System;

namespace RTSFramework.Contracts.Models.TestExecution
{
	public interface ITestCaseResult<TTestCase> where TTestCase : ITestCase
	{
		string DisplayName { get; }
		TTestCase TestCase { get; set; }
		string ErrorMessage { get; }
		string StackTrace { get; }
		double DurationInSeconds { get; }
		DateTimeOffset StartTime { get; }
		DateTimeOffset EndTime { get; }
		TestExecutionOutcome Outcome { get; }
    }
}