using System.Collections.Generic;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.CSharp.MSTest.Models
{
    public class MSTestExectionResult : ITestProcessingResult
    {
		public List<MSTestTestResult> TestcasesResults { get; } = new List<MSTestTestResult>();

    }
}