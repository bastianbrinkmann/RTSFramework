namespace RTSFramework.Contracts.Models
{
	public interface IProgramModel
	{
        string VersionId { get; }

        string RootPath { get; }

		GranularityLevel GranularityLevel { get; }
	}
}