namespace RTSFramework.Contracts.Models.Delta
{
    public interface IDelta
	{
		IProgramModel SourceModel { get; }

		IProgramModel TargetModel { get; }
    }
}