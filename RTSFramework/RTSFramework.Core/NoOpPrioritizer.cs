using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Core
{
	public class NoOpPrioritizer<TTestCase> : ITestPrioritizer<TTestCase> where TTestCase : ITestCase
	{
		public Task<IList<TTestCase>> PrioritizeTests(ISet<TTestCase> testCases, CancellationToken token)
		{
			IList<TTestCase> prioritizedTests = testCases.ToList();
			return Task.FromResult(prioritizedTests);
		}
	}
}