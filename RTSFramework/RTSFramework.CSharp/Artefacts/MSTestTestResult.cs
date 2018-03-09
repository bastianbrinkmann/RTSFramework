using System;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.CSharp.Artefacts
{
    public class MSTestTestResult : ITestCaseResult<MSTestTestcase>
    {
        public TestCaseResultType Outcome { get; set; }
        public string TestName { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public double DurationInSeconds { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string FullClassName { get; set; }
        public string AssemblyPathName { get; set; }

        public MSTestTestcase AssociatedTestCase { get; set; }
    }
}