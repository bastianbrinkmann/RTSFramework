using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
	public interface ITestsPrioritizer<TTestCase> where TTestCase : ITestCase
	{
		Task<IList<TTestCase>> PrioritizeTests(IList<TTestCase> testCases, CancellationToken token);
	}
}