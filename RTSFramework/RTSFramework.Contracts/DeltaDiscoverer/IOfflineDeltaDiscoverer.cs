using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.DeltaDiscoverer
{
	public interface IOfflineDeltaDiscoverer<TProgram, TProgramDelta> where TProgram : IProgramModel where TProgramDelta : IDelta<TProgram>
	{
		TProgramDelta Discover(TProgram oldVersion, TProgram newVersion);
	}
}