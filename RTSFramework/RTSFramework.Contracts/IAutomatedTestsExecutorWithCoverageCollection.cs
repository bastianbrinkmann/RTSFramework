using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
	public interface IAutomatedTestsExecutorWithCoverageCollection<TTc> : IAutomatedTestsExecutor<TTc> where TTc : ITestCase
	{
	    CoverageData GetCollectedCoverageData();
	}
}