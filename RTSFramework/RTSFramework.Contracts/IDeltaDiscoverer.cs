using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
	public interface IDeltaDiscoverer<TP, TPe, TD>where TP : IProgram where TD: IDelta<TPe, TP> where TPe : IProgramElement
	{
		
	}
}