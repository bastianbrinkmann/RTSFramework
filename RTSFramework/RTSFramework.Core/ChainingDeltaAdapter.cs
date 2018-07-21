using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Core
{
	public class ChainingDeltaAdapter<TSourceDelta, TIntermediateDelta, TTargetDelta, TSourceProgram, TIntermediateProgram, TTargetProgram> : IDeltaAdapter<TSourceDelta, TTargetDelta, TSourceProgram, TTargetProgram> 
		where TSourceDelta : IDelta<TSourceProgram>
		where TIntermediateDelta : IDelta<TIntermediateProgram>
		where TTargetDelta : IDelta<TTargetProgram> 
		where TSourceProgram : IProgramModel
		where TTargetProgram : IProgramModel 
		where TIntermediateProgram : IProgramModel
	{
		private readonly IDeltaAdapter<TSourceDelta, TIntermediateDelta, TSourceProgram, TIntermediateProgram> firstAdapter;
		private readonly IDeltaAdapter<TIntermediateDelta, TTargetDelta, TIntermediateProgram, TTargetProgram> secondAdapter;


		public ChainingDeltaAdapter(IDeltaAdapter<TSourceDelta, TIntermediateDelta, TSourceProgram, TIntermediateProgram> firstAdapter,
			IDeltaAdapter<TIntermediateDelta, TTargetDelta, TIntermediateProgram, TTargetProgram> secondAdapter)
		{
			this.firstAdapter = firstAdapter;
			this.secondAdapter = secondAdapter;
		}

		public TTargetDelta Convert(TSourceDelta deltaToConvert)
		{
			TIntermediateDelta intermediateDelta = firstAdapter.Convert(deltaToConvert);
			return secondAdapter.Convert(intermediateDelta);
		}
	}
}