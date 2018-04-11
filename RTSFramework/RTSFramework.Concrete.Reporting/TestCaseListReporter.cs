using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.Reporting
{
    public class TestCaseListReporter<TTestCase> : ITestProcessor<TTestCase> where TTestCase : ITestCase
    {
		public IEnumerable<TTestCase> IdentifiedTests { get; private set; }

	    public Task ProcessTests(IEnumerable<TTestCase> tests, CancellationToken cancellationToken = default(CancellationToken))
        {
	        IdentifiedTests = tests;

			return Task.CompletedTask;
		}
    }
}
