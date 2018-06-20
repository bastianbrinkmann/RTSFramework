namespace RTSFramework.Contracts.Models.Delta
{
    public interface IDelta<TModel>
	{
		TModel OldModel { get; }
		TModel NewModel { get; }
	}
}