using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
	public interface IDeltaDiscoverer<TP, TPe, TD>where TP : IProgram where TD: IDelta<TPe> where TPe : IProgramElement
	{
		
	}
}