using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.Git.Models
{
	public class GitVersionIdentification : ICSharpProgramArtefact
	{
		public string RepositoryPath { get; set; }

		public GitCommit Commit { get; set; } = new GitCommit();

		public GitVersionReferenceType ReferenceType { get; set; }

		public string AbsoluteSolutionPath { get; set; }

		public GranularityLevel GranularityLevel { get; set; }
	}
}