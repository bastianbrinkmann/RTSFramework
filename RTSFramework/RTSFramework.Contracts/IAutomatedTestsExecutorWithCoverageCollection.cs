using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
	public interface IAutomatedTestsExecutorWithCoverageCollection<TTc> : IAutomatedTestsExecutor<TTc> where TTc : ITestCase
	{
	    ICoverageData GetCollectedCoverageData();
	}
}