using System;
using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.CSharp.Artefacts
{
    public class MSTestTestResult : ITestCaseResult<MSTestTestcase>
    {
        public TestCaseResultType Outcome { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public double DurationInSeconds { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public MSTestTestcase AssociatedTestCase { get; set; }

        public List<ITestCaseResult<MSTestTestcase>> ChildrenResults { get; } = new List<ITestCaseResult<MSTestTestcase>>();
    }
}