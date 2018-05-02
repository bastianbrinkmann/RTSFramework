using System.Collections.Generic;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.TestExecution;

namespace RTSFramework.Concrete.CSharp.MSTest.Models
{
    public class MSTestExectionResult : ITestExecutionResult<MSTestTestcase>
    {
		public List<ITestCaseResult<MSTestTestcase>> TestcasesResults { get; } = new List<ITestCaseResult<MSTestTestcase>>();
    }
}