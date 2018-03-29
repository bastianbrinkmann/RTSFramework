using System;
using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.CSharp.Core.Artefacts
{
    public class MSTestTestResult : ITestCaseResult<MSTestTestcase>
    {
        public TestCaseResultType Outcome { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public double DurationInSeconds { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string TestCaseId { get; set; }

        public List<ITestCaseResult<MSTestTestcase>> ChildrenResults { get; } = new List<ITestCaseResult<MSTestTestcase>>();
    }
}