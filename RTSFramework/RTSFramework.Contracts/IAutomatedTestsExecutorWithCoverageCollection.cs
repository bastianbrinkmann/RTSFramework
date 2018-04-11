using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
	public interface IAutomatedTestsExecutorWithCoverageCollection<TTestCase> : IAutomatedTestsExecutor<TTestCase> where TTestCase : ITestCase
	{
	    CoverageData GetCollectedCoverageData();
	}
}