using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Concrete.Reporting{
    public class TestReporter<TTestCase, TDelta, TModel> : ITestProcessor<TTestCase, TestListResult<TTestCase>, TDelta, TModel> where TTestCase : ITestCase where TDelta : IDelta<TModel> where TModel : IProgramModel
    {
	    public Task<TestListResult<TTestCase>> ProcessTests(IList<TTestCase> impactedTests, ISet<TTestCase> allTests, TDelta impactedForDelta, CancellationToken cancellationToken)
		{
		    return Task.FromResult(new TestListResult<TTestCase> {IdentifiedTests = impactedTests});
	    }
    }
}
