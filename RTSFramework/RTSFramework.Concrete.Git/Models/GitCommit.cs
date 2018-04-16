using RTSFramework.Contracts.Utilities;

namespace RTSFramework.Concrete.Git.Models
{
	public class GitCommit
	{
		public string ShaId { get; set; }

		public string Message { get; set; }

		public string Committer { get; set; }
	}
}