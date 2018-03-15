using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
	public interface IAutomatedTestFrameworkWithCoverageCollection<TTc> : IAutomatedTestFramework<TTc> where TTc : ITestCase
	{
	    ICoverageData GetCollectedCoverageData();
	}
}