using System;
using System.Collections.Generic;
using System.Threading;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.RTSApproach;

namespace RTSFramework.RTSApproaches.Dynamic
{
    public class RetestAllApproach<TTc> : IRTSApproach<TTc> where TTc : ITestCase
    {
		public event EventHandler<ImpactedTestEventArgs<TTc>> ImpactedTest;

		public void ExecuteRTS(IEnumerable<TTc> testCases, StructuralDelta delta, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (TTc testcase in testCases)
            {
	            if (cancellationToken.IsCancellationRequested)
	            {
		            return;
	            }
                ImpactedTest?.Invoke(this, new ImpactedTestEventArgs<TTc>(testcase));
            }
        }
    }
}