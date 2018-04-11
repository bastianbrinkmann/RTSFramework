using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.DeltaDiscoverer
{
	public interface IDeltaDiscoverer<TModel, TDelta> where TModel : IProgramModel where TDelta : IDelta
	{
		
	}
}