using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.RTSApproaches.Static
{
	public class StaticTestSelector<TProgram, TInputProgramDelta, TSelectionProgramDelta, TTestCase, TDataStructure> : ITestSelector<TProgram, TInputProgramDelta, TTestCase>
		where TProgram : IProgramModel 
		where TInputProgramDelta : IDelta<TProgram>
		where TSelectionProgramDelta : IDelta<TProgram>
		where TTestCase : ITestCase
	{
		private readonly IDataStructureBuilder<TDataStructure, TProgram> dataStructureBuilder;
		private readonly IStaticRTS<TProgram, TSelectionProgramDelta, TTestCase, TDataStructure> staticSelector;
		private readonly IDeltaAdapter<TInputProgramDelta, TSelectionProgramDelta, TProgram> deltaAdapter;
		public ISet<TTestCase> SelectedTests { get; private set; }
		public StaticTestSelector(IDataStructureBuilder<TDataStructure, TProgram> dataStructureBuilder,
			IStaticRTS<TProgram, TSelectionProgramDelta, TTestCase, TDataStructure> staticSelector,
			IDeltaAdapter<TInputProgramDelta, TSelectionProgramDelta, TProgram> deltaAdapter)
		{
			this.dataStructureBuilder = dataStructureBuilder;
			this.staticSelector = staticSelector;
			this.deltaAdapter = deltaAdapter;
			this.deltaAdapter = deltaAdapter;
		}

		public async Task SelectTests(StructuralDelta<TestsModel<TTestCase>, TTestCase> testsDelta, TInputProgramDelta programDelta, CancellationToken cancellationToken)
		{
			var convertedDelta = deltaAdapter.Convert(programDelta);

			var dataStructure = await dataStructureBuilder.GetDataStructure(convertedDelta.NewModel, cancellationToken);

			SelectedTests = staticSelector.SelectTests(dataStructure, testsDelta, convertedDelta, cancellationToken);
		}

		public ICorrespondenceModel CorrespondenceModel => staticSelector.CorrespondenceModel;
	}
}