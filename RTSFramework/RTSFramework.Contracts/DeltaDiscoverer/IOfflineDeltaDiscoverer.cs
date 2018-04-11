using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.DeltaDiscoverer
{
	public interface IOfflineDeltaDiscoverer : IDeltaDiscoverer
	{
		StructuralDelta Discover(IProgramModel oldVersion, IProgramModel newVersion);
	}
}