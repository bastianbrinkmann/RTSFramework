using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.RTSApproaches.Static
{
	public class StaticTestSelector<TModel, TDelta, TTestCase, TDataStructure> : ITestSelector<TModel, TDelta, TTestCase> where TModel : IProgramModel where TDelta : IDelta<TModel> where TTestCase : ITestCase
	{
		private readonly IDataStructureProvider<TDataStructure, TModel> dataStructureProvider;
		private readonly IDeltaExpander<TModel, TTestCase, TDelta, TDataStructure> deltaExpander;
		public ISet<TTestCase> SelectedTests { private set; get; }
		public Func<string, IList<string>> GetResponsibleChangesByTestId { private set; get; }

		public StaticTestSelector(IDataStructureProvider<TDataStructure, TModel> dataStructureProvider, IDeltaExpander<TModel, TTestCase, TDelta, TDataStructure>  deltaExpander)
		{
			this.dataStructureProvider = dataStructureProvider;
			this.deltaExpander = deltaExpander;
		}

		public async Task SelectTests(ISet<TTestCase> testCases, TDelta delta, CancellationToken cancellationToken)
		{
			var dataStructure = await dataStructureProvider.GetDataStructureForProgram(delta.NewModel, cancellationToken);

			await deltaExpander.ExpandDelta(testCases, delta, dataStructure, cancellationToken);

			GetResponsibleChangesByTestId = deltaExpander.GetResponsibleChangesByTestId;
			SelectedTests = deltaExpander.SelectedTests;
		}
	}
}