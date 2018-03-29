using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.DeltaDiscoverer
{
	public interface IOnlineDeltaDiscoverer<TP, TD> : IDeltaDiscoverer<TP, TD> where TD : IDelta<TP> where TP : IProgramModel
	{
		TD GetCurrentDelta();

		void StartDiscovery(TP startingVersion);

		void StopDiscovery();
	}
}