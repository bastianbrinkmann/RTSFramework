using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
	public interface IOnlineDeltaDiscoverer<TP, TPe, TD> : IDeltaDiscoverer<TP, TPe, TD> where TD : IDelta<TPe, TP> where TPe : IProgramElement where TP : IProgram
	{
		TD GetCurrentDelta();

		void StartDiscovery(TP startingVersion);

		void StopDiscovery();
	}
}