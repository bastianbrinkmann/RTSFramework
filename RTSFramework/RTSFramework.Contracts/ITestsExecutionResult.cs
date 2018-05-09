using System.Collections.Generic;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.TestExecution;

namespace RTSFramework.Contracts
{
	public interface ITestsExecutionResult<TTestCase> : ITestProcessingResult where TTestCase : ITestCase
	{
		List<ITestCaseResult<TTestCase>> TestcasesResults { get; }
	}
}