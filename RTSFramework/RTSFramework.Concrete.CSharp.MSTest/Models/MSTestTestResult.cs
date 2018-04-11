using System;
using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.CSharp.MSTest.Models
{
    public class MSTestTestResult : ITestCaseResult<MSTestTestcase>
    {
        public TestExecutionOutcome Outcome { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public double DurationInSeconds { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string TestCaseId { get; set; }

        public List<ITestCaseResult<MSTestTestcase>> ChildrenResults { get; } = new List<ITestCaseResult<MSTestTestcase>>();
    }
}