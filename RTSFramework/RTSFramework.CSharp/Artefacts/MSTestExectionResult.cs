using System.Collections.Generic;

namespace RTSFramework.Concrete.CSharp.Artefacts
{
    public class MSTestExectionResult
    {
        public List<MSTestTestResult> TestcasesResults { get; } = new List<MSTestTestResult>();

        public string CodeCoverageFile { get; set; }
    }
}