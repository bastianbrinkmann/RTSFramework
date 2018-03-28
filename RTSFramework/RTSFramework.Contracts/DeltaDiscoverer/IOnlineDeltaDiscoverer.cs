using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.Delta;

namespace RTSFramework.Contracts.DeltaDiscoverer
{
	public interface IOnlineDeltaDiscoverer<TP, TD> : IDeltaDiscoverer<TP, TD> where TD : IDelta where TP : IProgramModel
	{
		TD GetCurrentDelta();

		void StartDiscovery(TP startingVersion);

		void StopDiscovery();
	}
}