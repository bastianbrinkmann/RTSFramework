﻿using System;
using System.Collections.Generic;
using System.Threading;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.RTSApproaches.Core
{
	public class RetestAllSelector<TModel, TDelta, TTestCase> : ITestSelector<TModel, TDelta, TTestCase>
		where TModel : IProgramModel
		where TDelta : IDelta<TModel>
		where TTestCase : ITestCase
	{
		public event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;

		public void SelectTests(IEnumerable<TTestCase> testCases, TDelta delta, CancellationToken cancellationToken)
		{
			foreach (TTestCase testcase in testCases)
			{
				cancellationToken.ThrowIfCancellationRequested();
				ImpactedTest?.Invoke(this, new ImpactedTestEventArgs<TTestCase>(testcase));
			}
		}

		public void UpdateInternalDataStructure(ITestProcessingResult processingResult, CancellationToken token)
		{
			
		}
	}
}