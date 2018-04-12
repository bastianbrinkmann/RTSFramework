using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.TFS2010
{
	public class TFS2010VersionIdentification : ICSharpProgramArtefact
	{
		public string CommitId { get; set; }

		public string AbsoluteSolutionPath { get; set; }

		public GranularityLevel GranularityLevel { get; set; }
	}
}