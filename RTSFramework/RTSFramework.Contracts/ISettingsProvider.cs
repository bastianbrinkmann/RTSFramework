namespace RTSFramework.Contracts
{
	public interface ISettingsProvider
	{
		string Configuration { get; }
		string Platform { get; }
		string TestAssembliesFilter { get; }
		bool CleanupTestResultsDirectory { get; }
		bool LogToFile { get; }
	}
}