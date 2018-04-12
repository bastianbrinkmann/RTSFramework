namespace RTSFramework.Contracts.Models.Delta
{
    public interface IDelta<TModel> where TModel : IProgramModel
	{
		TModel SourceModel { get; }
		TModel TargetModel { get; }
	}
}