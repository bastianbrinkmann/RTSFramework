using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Core
{
	public class NoOpPrioritizer<TTestCase> : ITestPrioritizer<TTestCase> where TTestCase : ITestCase
	{
		public Task<IList<TTestCase>> PrioritizeTests(IList<TTestCase> testCases, CancellationToken token)
		{
			return Task.FromResult(testCases);
		}
	}
}