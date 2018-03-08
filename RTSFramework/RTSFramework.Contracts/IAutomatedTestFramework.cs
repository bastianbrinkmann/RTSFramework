using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
	public interface IAutomatedTestFramework<TTC> : ITestFramework<TTC> where TTC : ITestCase
	{
		IEnumerable<ITestCaseResult<TTC>> ExecuteTests(IEnumerable<TTC> tests);
	}
}