using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
	public interface ITestsDiscoverer<TTC> where TTC : ITestCase
	{
        IEnumerable<string> Sources { set; }

        IEnumerable<TTC> GetTestCases();
	}
}