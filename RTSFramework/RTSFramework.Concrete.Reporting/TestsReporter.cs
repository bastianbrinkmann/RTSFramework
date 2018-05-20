﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Concrete.Reporting
{
    public class TestsReporter<TTestCase, TDelta, TModel> : ITestsProcessor<TTestCase, TestListResult<TTestCase>, TDelta, TModel> where TTestCase : ITestCase where TDelta : IDelta<TModel> where TModel : IProgramModel
    {
	    public Task<TestListResult<TTestCase>> ProcessTests(IList<TTestCase> impactedTests, IList<TTestCase> allTests, TDelta impactedForDelta, CancellationToken cancellationToken)
		{
		    return Task.FromResult(new TestListResult<TTestCase> {IdentifiedTests = impactedTests});
	    }
    }
}