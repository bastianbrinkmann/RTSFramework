using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.DeltaDiscoverer
{
	public interface IOnlineDeltaDiscoverer
	{
		IDelta GetCurrentDelta();

		void StartDiscovery(IProgramModel startingVersion);

		void StopDiscovery();
	}
}