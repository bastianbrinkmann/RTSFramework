using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
	public interface ITestPrioritizer<TTestCase> where TTestCase : ITestCase
	{
		Task<IList<TTestCase>> PrioritizeTests(ISet<TTestCase> testCases, CancellationToken token);
	}
}