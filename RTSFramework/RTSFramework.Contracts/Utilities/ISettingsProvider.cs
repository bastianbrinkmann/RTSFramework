namespace RTSFramework.Contracts.Utilities
{
	public interface ISettingsProvider
	{
		string Configuration { get; }
		string Platform { get; }
		string TestAssembliesFilter { get; }
		bool CleanupTestResultsDirectory { get; }
		bool LogToFile { get; }

		string[] AdditionalReferences { get; }
	}
}