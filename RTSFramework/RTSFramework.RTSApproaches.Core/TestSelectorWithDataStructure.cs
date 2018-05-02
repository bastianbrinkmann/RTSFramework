using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

		public async Task<IList<TTestCase>> SelectTests(IList<TTestCase> testCases, TDelta delta, CancellationToken cancellationToken)
		{
			TDataStructure dataStructure = await DataStructureProvider.GetDataStructureForProgram(delta.SourceModel, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			return await SelectTests(dataStructure, testCases, delta, cancellationToken);
		}

		protected abstract Task<IList<TTestCase>> SelectTests(TDataStructure dataStructure, IList<TTestCase> testCases, TDelta delta, CancellationToken cancellationToken);
	}
}