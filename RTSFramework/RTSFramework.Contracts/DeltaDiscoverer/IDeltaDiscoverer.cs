using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.Delta;

namespace RTSFramework.Contracts.DeltaDiscoverer
{
	public interface IDeltaDiscoverer<TP, TD> where TP : IProgramModel where TD: IDelta
	{
		
	}
}