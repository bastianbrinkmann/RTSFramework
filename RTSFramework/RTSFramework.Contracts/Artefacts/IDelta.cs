namespace RTSFramework.Contracts.Artefacts
{
    public interface IDelta<TPe, TP> where TPe : IProgramElement where TP : IProgram
	{
	    TP Source { get; }
	}
}