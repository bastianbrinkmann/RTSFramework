using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Concrete.Reporting{
    public class TestReporter<TTestCase, TProgramDelta, TProgram> : ITestProcessor<TTestCase, TestListResult<TTestCase>, TProgramDelta, TProgram> where TTestCase : ITestCase where TProgramDelta : IDelta<TProgram> where TProgram : IProgramModel
    {
	    public Task<TestListResult<TTestCase>> ProcessTests(IList<TTestCase> impactedTests, StructuralDelta<TestsModel<TTestCase>, TTestCase> testsDelta, TProgramDelta programDelta, CancellationToken cancellationToken)
		{
		    return Task.FromResult(new TestListResult<TTestCase> {IdentifiedTests = impactedTests});
	    }
    }
}
