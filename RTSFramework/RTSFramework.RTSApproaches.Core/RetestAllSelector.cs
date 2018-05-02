﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
		public Task<IList<TTestCase>> SelectTests(IList<TTestCase> testCases, TDelta delta, CancellationToken cancellationToken)
		{
			return Task.FromResult(testCases);
		}
	}
}