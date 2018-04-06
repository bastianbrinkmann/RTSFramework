using System;
using System.Collections.Generic;

namespace RTSFramework.Contracts.Models
{
	public interface ITestCaseResult<TTC> where TTC : ITestCase
	{
        string TestCaseId { get; }

		string ErrorMessage { get; }
		string StackTrace { get; }
		double DurationInSeconds { get; }
		DateTimeOffset StartTime { get; }
		DateTimeOffset EndTime { get; }

		TestCaseResultType Outcome { get; }

        List<ITestCaseResult<TTC>> ChildrenResults { get; }
    }
}