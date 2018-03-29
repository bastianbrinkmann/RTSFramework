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
		DateTime StartTime { get; }
		DateTime EndTime { get; }

		TestCaseResultType Outcome { get; }

        List<ITestCaseResult<TTC>> ChildrenResults { get; }
    }
}