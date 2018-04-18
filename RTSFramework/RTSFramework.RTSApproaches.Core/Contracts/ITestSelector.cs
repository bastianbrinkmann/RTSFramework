using System;
using System.Collections.Generic;
using System.Threading;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.RTSApproaches.Core.Contracts
{
	public interface ITestSelector<TModel, TDelta, TTestCase>
		where TModel : IProgramModel
		where TDelta : IDelta<TModel>
		where TTestCase : ITestCase
	{
		event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;

		void SelectTests(IEnumerable<TTestCase> testCases, TDelta delta, CancellationToken cancellationToken);

		void UpdateInternalDataStructure(ITestProcessingResult processingResult, CancellationToken token);
	}
}