using System.Collections.Generic;

namespace RTSFramework.Concrete.CSharp.MSTest.Models
{
    public class MSTestExectionResult
    {
        public List<MSTestTestResult> TestcasesResults { get; } = new List<MSTestTestResult>();

        public string CodeCoverageFile { get; set; }
    }
}