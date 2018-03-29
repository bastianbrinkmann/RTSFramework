using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
	public interface ITestsDiscoverer<TTC> where TTC : ITestCase
	{
        IEnumerable<string> Sources { set; }

        IEnumerable<TTC> GetTestCases();
	}
}