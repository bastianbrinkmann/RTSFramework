﻿using System;

namespace RTSFramework.Contracts.Artefacts
{
	public interface ITestCaseResult<TTC> where TTC : ITestCase
	{
		TTC AssociatedTestCase { get; }

		string ErrorMessage { get; }
		string StackTrace { get; }
		double DurationInSeconds { get; }
		DateTime StartTime { get; }
		DateTime EndTime { get; }

		TestCaseResultType Outcome { get; }
	}
}