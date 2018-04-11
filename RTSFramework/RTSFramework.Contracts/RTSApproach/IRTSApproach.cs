using System;
using System.Collections.Generic;
using System.Threading;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.RTSApproach
{
    public interface IRTSApproach<TTc>where TTc : ITestCase
    {
	    event EventHandler<ImpactedTestEventArgs<TTc>> ImpactedTest;
        void ExecuteRTS(IEnumerable<TTc> testCases, StructuralDelta delta, CancellationToken token = default(CancellationToken));

    }
}