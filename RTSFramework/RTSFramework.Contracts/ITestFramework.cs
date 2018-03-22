using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
	public interface ITestFramework<TTC> where TTC : ITestCase
	{
        IEnumerable<string> Sources { get; set; }

        IEnumerable<TTC> GetTestCases();
	}
}