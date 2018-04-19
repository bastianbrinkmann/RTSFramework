using System;
using System.Collections.Generic;
using System.Linq;

namespace RTSFramework.Contracts.Models
{
	public class CompositeTestCaseResult<TTestCase> : ITestCaseResult<TTestCase> where TTestCase : ITestCase
	{
		public List<ITestCaseResult<TTestCase>> ChildrenResults { get; } = new List<ITestCaseResult<TTestCase>>();
		public TTestCase TestCase { get; set; }
		public string ErrorMessage => null;
		public string StackTrace => null;
		public double DurationInSeconds => ChildrenResults.Sum(x => x.DurationInSeconds);
		public DateTimeOffset StartTime => ChildrenResults.Min(x => x.StartTime);
		public DateTimeOffset EndTime => ChildrenResults.Max(x => x.EndTime);

		public TestExecutionOutcome Outcome
			=> ChildrenResults.TrueForAll(x => x.Outcome == TestExecutionOutcome.Passed) ? TestExecutionOutcome.Passed : TestExecutionOutcome.Failed;
	}
}