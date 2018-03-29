namespace RTSFramework.Contracts.Models.Delta
{
    public interface IDelta
	{
        string SourceModelId { get; }

        string TargetModelId { get; }
    }
}