using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.DeltaDiscoverer
{
	public interface IDeltaDiscoverer<TP, TD> where TP : IProgramModel where TD: IDelta
	{
		
	}
}