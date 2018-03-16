using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
	public interface IAutomatedTestFramework<TTc> : ITestFramework<TTc> where TTc : ITestCase
	{
		IEnumerable<ITestCaseResult<TTc>> ExecuteTests(IEnumerable<TTc> tests);
    }
}