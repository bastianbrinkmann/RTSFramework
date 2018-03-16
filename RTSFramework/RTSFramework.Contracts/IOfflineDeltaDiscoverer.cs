using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.Delta;

namespace RTSFramework.Contracts
{
	public interface IOfflineDeltaDiscoverer<TP, TD> : IDeltaDiscoverer<TP, TD> where TD : IDelta where TP : IProgramModel
	{
		TD Discover(TP oldVersion, TP newVersion);
	}
}