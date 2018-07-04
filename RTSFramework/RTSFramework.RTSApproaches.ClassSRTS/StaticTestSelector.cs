using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.RTSApproaches.Core.Contracts;
using RTSFramework.RTSApproaches.Core.DataStructures;

namespace RTSFramework.RTSApproaches.Static
{
	public class StaticTestSelector<TModel, TInputDelta, TSelectionDelta, TTestCase, TDataStructure> : ITestSelector<TModel, TInputDelta, TTestCase>
		where TModel : IProgramModel 
		where TInputDelta : IDelta<TModel>
		where TSelectionDelta : IDelta<TModel>
		where TTestCase : ITestCase
	{
		private readonly IDataStructureBuilder<TDataStructure, TModel> dataStructureBuilder;
		private readonly IStaticRTS<TModel, TSelectionDelta, TTestCase, TDataStructure> staticSelector;
		private readonly IDeltaAdapter<TInputDelta, TSelectionDelta, TModel> deltaAdapter;
		public ISet<TTestCase> SelectedTests { get; private set; }
		public StaticTestSelector(IDataStructureBuilder<TDataStructure, TModel> dataStructureBuilder,
			IStaticRTS<TModel, TSelectionDelta, TTestCase, TDataStructure> staticSelector,
			IDeltaAdapter<TInputDelta, TSelectionDelta, TModel> deltaAdapter)
		{
			this.dataStructureBuilder = dataStructureBuilder;
			this.staticSelector = staticSelector;
			this.deltaAdapter = deltaAdapter;
			this.deltaAdapter = deltaAdapter;
		}

		public async Task SelectTests(StructuralDelta<TestsModel<TTestCase>, TTestCase> testsDelta, TInputDelta programDelta, CancellationToken cancellationToken)
		{
			var convertedDelta = deltaAdapter.Convert(programDelta);

			var dataStructure = await dataStructureBuilder.GetDataStructure(convertedDelta.NewModel, cancellationToken);

			SelectedTests = staticSelector.SelectTests(dataStructure, testsDelta, convertedDelta, cancellationToken);
		}

		public ICorrespondenceModel CorrespondenceModel => staticSelector.CorrespondenceModel;
	}
}