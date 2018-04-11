using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
	public interface IAutomatedTestsExecutor<TTestCase> : ITestProcessor<TTestCase> where TTestCase : ITestCase
	{
	    IEnumerable<ITestCaseResult<TTestCase>> GetResults();
	}
}