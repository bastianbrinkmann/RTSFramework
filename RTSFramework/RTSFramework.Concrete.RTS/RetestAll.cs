using System;
using System.Collections.Generic;
using System.Threading;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.RTSApproach;

namespace RTSFramework.RTSApproaches.Dynamic
{
    public class RetestAll<TModel, TDelta, TTestCase> : IRTSApproach<TModel, TDelta, TTestCase> where TTestCase : ITestCase where TDelta : IDelta<TModel> where TModel : IProgramModel
    {
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;

		public void ExecuteRTS(IEnumerable<TTestCase> testCases, TDelta delta, CancellationToken cancellationToken)
        {
            foreach (TTestCase testcase in testCases)
            {
				cancellationToken.ThrowIfCancellationRequested();
				ImpactedTest?.Invoke(this, new ImpactedTestEventArgs<TTestCase>(testcase));
            }
        }
    }
}