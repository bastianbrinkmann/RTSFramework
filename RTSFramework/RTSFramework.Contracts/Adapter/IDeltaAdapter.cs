using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.Adapter
{
	public interface IDeltaAdapter<TSourceDelta, TTargetDelta, TModel> 
		where TSourceDelta : IDelta<TModel>
		where TTargetDelta : IDelta<TModel>
		where TModel : IProgramModel
	{
		TTargetDelta Convert(TSourceDelta deltaToConvert);
	}
}