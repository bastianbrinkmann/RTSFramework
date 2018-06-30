using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.RTSApproaches.Core.Contracts;
using RTSFramework.RTSApproaches.Core.DataStructures;

namespace RTSFramework.RTSApproaches.Static
{
	public class StaticTestSelector<TModel, TDelta, TTestCase, TDataStructure> : ITestSelector<TModel, TDelta, TTestCase>
		where TModel : IProgramModel 
		where TDelta : IDelta<TModel>
		where TTestCase : ITestCase
	{
		private readonly IDataStructureProvider<TDataStructure, TModel> dataStructureProvider;
		private readonly IStaticRTS<TModel, TDelta, TTestCase, TDataStructure> staticSelector;
		public ISet<TTestCase> SelectedTests { get; private set; }
		public StaticTestSelector(IDataStructureProvider<TDataStructure, TModel> dataStructureProvider,
			IStaticRTS<TModel, TDelta, TTestCase, TDataStructure> staticSelector)
		{
			this.dataStructureProvider = dataStructureProvider;
			this.staticSelector = staticSelector;
		}

		public async Task SelectTests(StructuralDelta<ISet<TTestCase>, TTestCase> testsDelta, TDelta delta, CancellationToken cancellationToken)
		{
			var dataStructure = await dataStructureProvider.GetDataStructure(delta.NewModel, cancellationToken);

			SelectedTests = staticSelector.SelectTests(dataStructure, testsDelta, delta, cancellationToken);
		}

		public ICorrespondenceModel CorrespondenceModel => null;
	}
}