using System;
using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.CSharp.MSTest.Models
{
    public class MSTestTestResult : ITestCaseResult<MSTestTestcase>
    {
        public TestCaseResultType Outcome { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public double DurationInSeconds { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public string TestCaseId { get; set; }

        public List<ITestCaseResult<MSTestTestcase>> ChildrenResults { get; } = new List<ITestCaseResult<MSTestTestcase>>();
    }
}