using System;
using System.Collections.Generic;
using System.Threading;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.RTSApproaches.Core
{
	public abstract class TestSelectorWithDataStructure<TModel, TDelta, TTestCase, TDataStructure> : ITestSelector<TModel, TDelta, TTestCase>
		where TModel : IProgramModel
		where TDelta : IDelta<TModel>
		where TTestCase : ITestCase
	{
		protected readonly IDataStructureProvider<TDataStructure, TModel> DataStructureProvider;

		public TestSelectorWithDataStructure(IDataStructureProvider<TDataStructure, TModel> dataStructureProvider)
		{
			DataStructureProvider = dataStructureProvider;
		}

		public abstract event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
		public void SelectTests(IEnumerable<TTestCase> testCases, TDelta delta, CancellationToken cancellationToken)
		{
			TDataStructure dataStructure = DataStructureProvider.GetDataStructureForProgram(delta.SourceModel, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			SelectTests(dataStructure, testCases, delta, cancellationToken);
		}

		protected abstract void SelectTests(TDataStructure dataStructure, IEnumerable<TTestCase> testCases, TDelta delta, CancellationToken cancellationToken);

		public virtual void UpdateInternalDataStructure(ITestProcessingResult processingResult, CancellationToken token)
		{
			
		}
	}
}