using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.Reporting
{
    public class IdentifiedTestsListReporter<TTestCase> : ITestProcessor<TTestCase, TestListResult<TTestCase>> where TTestCase : ITestCase
    {
	    public Task<TestListResult<TTestCase>> ProcessTests(IEnumerable<TTestCase> tests, CancellationToken cancellationToken)
	    {
		    return Task.FromResult(new TestListResult<TTestCase> {IdentifiedTests = tests});
	    }
    }
}
