using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Core
{
	public class IdentityDeltaAdapter<TDelta, TModel> : IDeltaAdapter<TDelta, TDelta, TModel>
		where TDelta : IDelta<TModel>
		where TModel : IProgramModel
	{
		public TDelta Convert(TDelta deltaToConvert)
		{
			return deltaToConvert;
		}
	}
}