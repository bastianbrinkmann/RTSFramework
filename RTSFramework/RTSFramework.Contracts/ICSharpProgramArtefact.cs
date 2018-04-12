using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
	public interface ICSharpProgramArtefact
	{
		GranularityLevel GranularityLevel { get; set; }

		string AbsoluteSolutionPath { get; set; }
	}
}