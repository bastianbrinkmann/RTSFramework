using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.RTSApproaches.Core.Contracts;
using RTSFramework.RTSApproaches.Core.DataStructures;

namespace RTSFramework.RTSApproaches.Static
{
	public class StaticTestSelector<TModel, TDelta, TTestCase> : ITestSelector<TModel, TDelta, TTestCase>
		where TModel : IProgramModel 
		where TDelta : IDelta<TModel>
		where TTestCase : ITestCase
	{
		private readonly IDataStructureProvider<IntertypeRelationGraph, TModel> irgBuilder;
		private readonly IStaticRTS<TModel, TDelta, TTestCase, IntertypeRelationGraph> staticSelector;
		public ISet<TTestCase> SelectedTests { get; private set; }
		public Func<string, IList<string>> GetResponsibleChangesByTestId { get; private set; }
		public StaticTestSelector(IDataStructureProvider<IntertypeRelationGraph, TModel> irgBuilder,
			IStaticRTS<TModel, TDelta, TTestCase, IntertypeRelationGraph> staticSelector)
		{
			this.irgBuilder = irgBuilder;
			this.staticSelector = staticSelector;
		}

		public async Task SelectTests(ISet<TTestCase> testCases, TDelta delta, CancellationToken cancellationToken)
		{
			//Using the IRG for P' is possible as it is built using the intermediate language
			//Therefore, the program at least compiles - preventing issues from for example deleted files
			var graph = await irgBuilder.GetDataStructureForProgram(delta.NewModel, cancellationToken);

			SelectedTests = staticSelector.SelectTests(graph, testCases, delta, cancellationToken);
			GetResponsibleChangesByTestId = staticSelector.GetResponsibleChangesByTestId;
		}
	}
}