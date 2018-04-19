using System.Collections.Generic;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.CSharp.MSTest.Models
{
    public class MSTestExectionResult : ITestProcessingResult
    {
		public List<ITestCaseResult<MSTestTestcase>> TestcasesResults { get; } = new List<ITestCaseResult<MSTestTestcase>>();
    }
}