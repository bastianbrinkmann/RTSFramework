namespace RTSFramework.Contracts.Models.Delta
{
    public interface IDelta<TModel> where TModel : IProgramModel
	{
		TModel OldModel { get; }
		TModel NewModel { get; }
	}
}