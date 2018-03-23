using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
	public interface IAutomatedTestsExecutor<TTc> : ITestProcessor<TTc> where TTc : ITestCase
	{
	    IEnumerable<ITestCaseResult<TTc>> GetResults();
	}
}