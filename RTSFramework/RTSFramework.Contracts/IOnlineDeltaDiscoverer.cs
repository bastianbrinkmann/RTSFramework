using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.Delta;

namespace RTSFramework.Contracts
{
	public interface IOnlineDeltaDiscoverer<TP, TD> : IDeltaDiscoverer<TP, TD> where TD : IDelta where TP : IProgramModel
	{
		TD GetCurrentDelta();

		void StartDiscovery(TP startingVersion);

		void StopDiscovery();
	}
}