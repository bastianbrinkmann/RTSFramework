using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
	public interface ICSharpProgramArtefact
	{
		string AbsoluteSolutionPath { get; set; }
	}
}