using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts.Delta
{
    public interface IDelta
	{
        string SourceModelId { get; }

        string TargetModelId { get; }
    }
}