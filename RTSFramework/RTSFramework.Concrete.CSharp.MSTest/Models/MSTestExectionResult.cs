using System.Collections.Generic;
using RTSFramework.Concrete.CSharp.Core.Artefacts;

namespace RTSFramework.Concrete.CSharp.MSTest.Models
{
    public class MSTestExectionResult
    {
        public List<MSTestTestResult> TestcasesResults { get; } = new List<MSTestTestResult>();

        public string CodeCoverageFile { get; set; }
    }
}