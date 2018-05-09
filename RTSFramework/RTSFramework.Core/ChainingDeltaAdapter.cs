using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Core
{
	public class ChainingDeltaAdapter<TSourceDelta, TIntermediateDelta, TTargetDelta, TModel> : IDeltaAdapter<TSourceDelta, TTargetDelta, TModel> 
		where TSourceDelta : IDelta<TModel>
		where TIntermediateDelta : IDelta<TModel>
		where TTargetDelta : IDelta<TModel> 
		where TModel : IProgramModel 
	{
		private readonly IDeltaAdapter<TSourceDelta, TIntermediateDelta, TModel> firstAdapter;
		private readonly IDeltaAdapter<TIntermediateDelta, TTargetDelta, TModel> secondAdapter;


		public ChainingDeltaAdapter(IDeltaAdapter<TSourceDelta, TIntermediateDelta, TModel> firstAdapter,
			IDeltaAdapter<TIntermediateDelta, TTargetDelta, TModel> secondAdapter)
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