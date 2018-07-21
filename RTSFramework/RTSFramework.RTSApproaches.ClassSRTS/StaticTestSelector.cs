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
	public class StaticTestSelector<TInputProgram, TSelectionProgram, TInputProgramDelta, TSelectionProgramDelta, TTestCase, TDataStructure> : ITestSelector<TInputProgram, TInputProgramDelta, TTestCase>
		where TInputProgram : IProgramModel
		where TSelectionProgram : IProgramModel
		where TInputProgramDelta : IDelta<TInputProgram>
		where TSelectionProgramDelta : IDelta<TSelectionProgram>
		where TTestCase : ITestCase
	{
		private readonly IDataStructureBuilder<TDataStructure, TSelectionProgram> dataStructureBuilder;
		private readonly IStaticRTS<TSelectionProgram, TSelectionProgramDelta, TTestCase, TDataStructure> staticSelector;
		private readonly IDeltaAdapter<TInputProgramDelta, TSelectionProgramDelta, TInputProgram, TSelectionProgram> deltaAdapter;
		public ISet<TTestCase> SelectedTests { get; private set; }
		public StaticTestSelector(IDataStructureBuilder<TDataStructure, TSelectionProgram> dataStructureBuilder,
			IStaticRTS<TSelectionProgram, TSelectionProgramDelta, TTestCase, TDataStructure> staticSelector,
			IDeltaAdapter<TInputProgramDelta, TSelectionProgramDelta, TInputProgram, TSelectionProgram> deltaAdapter)
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