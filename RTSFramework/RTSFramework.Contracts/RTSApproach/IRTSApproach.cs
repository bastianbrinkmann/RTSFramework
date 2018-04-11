using System;
using System.Collections.Generic;
using System.Threading;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.RTSApproach
{
    public interface IRTSApproach<TDelta, TTestCase>where TTestCase : ITestCase where TDelta :IDelta
    {
	    event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
        void ExecuteRTS(IEnumerable<TTestCase> testCases, TDelta delta, CancellationToken token = default(CancellationToken));

    }
}