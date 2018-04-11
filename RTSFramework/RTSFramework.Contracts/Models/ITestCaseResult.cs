using System;
using System.Collections.Generic;

namespace RTSFramework.Contracts.Models
{
	public interface ITestCaseResult<TTestCase> where TTestCase : ITestCase
	{
        string TestCaseId { get; }

		string ErrorMessage { get; }
		string StackTrace { get; }
		double DurationInSeconds { get; }
		DateTime StartTime { get; }
		DateTime EndTime { get; }

		TestCaseResultType Outcome { get; }

        List<ITestCaseResult<TTestCase>> ChildrenResults { get; }
    }
}