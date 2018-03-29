namespace RTSFramework.Contracts.Models.Delta
{
    public interface IDelta<TP> where TP: IProgramModel
	{
        TP SourceModel { get; }

        TP TargetModel { get; }
    }
}