using System;
using System.Collections.Generic;

namespace RTSFramework.Contracts.Models
{
	public interface ITestCaseResult<TTestCase> where TTestCase : ITestCase
	{
		TTestCase TestCase { get; }

		string ErrorMessage { get; }
		string StackTrace { get; }
		double DurationInSeconds { get; }
		DateTime StartTime { get; }
		DateTime EndTime { get; }

		TestExecutionOutcome Outcome { get; }

        List<ITestCaseResult<TTestCase>> ChildrenResults { get; }
    }
}