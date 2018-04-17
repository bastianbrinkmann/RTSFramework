using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.DeltaDiscoverer
{
	public interface IOfflineDeltaDiscoverer<TModel, TDelta> where TModel : IProgramModel where TDelta : IDelta<TModel>
	{
		TDelta Discover(TModel oldVersion, TModel newVersion);
	}
}