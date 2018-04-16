using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using RTSFramework.Concrete.Git.Models;

namespace RTSFramework.Concrete.Git
{
	public class GitCommitsProvider
	{
		public IList<GitCommit> GetAllCommits(string repositoryPath)
		{
			if (!Repository.IsValid(repositoryPath))
			{
				return new List<GitCommit>();
			}

			using (var repo = new Repository(repositoryPath))
			{
				return repo.Commits.Select(x => new GitCommit
				{
					ShaId = x.Id.Sha,
					Message = x.Message,
					Committer = x.Committer.Name
				}).ToList();
			}
		}
	}
}