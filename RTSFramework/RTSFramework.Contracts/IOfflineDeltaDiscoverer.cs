using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
	public interface IOfflineDeltaDiscoverer<TP, TPe, TD> : IDeltaDiscoverer<TP, TPe, TD> where TD : IDelta<TPe, TP> where TP : IProgram where TPe : IProgramElement
	{
		TD Discover(TP oldVersion, TP newVersion);
	}
}