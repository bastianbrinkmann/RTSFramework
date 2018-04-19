using System;
using System.Collections.Generic;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.TestExecution;

namespace RTSFramework.Concrete.CSharp.MSTest.Models
{
    public class MSTestTestResult : ITestCaseResult<MSTestTestcase>
    {
        public virtual TestExecutionOutcome Outcome { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public double DurationInSeconds { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public MSTestTestcase TestCase { get; set; }

    }
}