using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
	public interface IAutomatedTestsExecutor<TTc> : ITestProcessor<TTc> where TTc : ITestCase
	{
	    IEnumerable<ITestCaseResult<TTc>> GetResults();
	}
}