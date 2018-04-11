﻿using System;
using System.Collections.Generic;
using System.Threading;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.RTSApproach;

namespace RTSFramework.RTSApproaches.Dynamic
{
    public class RetestAllApproach<TDelta, TTestCase> : IRTSApproach<TDelta, TTestCase> where TTestCase : ITestCase where TDelta : IDelta
    {
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;

		public void ExecuteRTS(IEnumerable<TTestCase> testCases, TDelta delta, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (TTestCase testcase in testCases)
            {
	            if (cancellationToken.IsCancellationRequested)
	            {
		            return;
	            }
                ImpactedTest?.Invoke(this, new ImpactedTestEventArgs<TTestCase>(testcase));
            }
        }
    }
}