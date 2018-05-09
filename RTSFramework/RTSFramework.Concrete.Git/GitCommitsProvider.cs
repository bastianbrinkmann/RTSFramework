using System;
using System.Collections.Generic;
using System.IO;
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

		public string GetCommitIdentifier(string repositoryPath, string commitId)
		{
			return $"GitRepo_{new DirectoryInfo(repositoryPath).Name}_{commitId}";
		}

		public string GetLatestCommitSha(string repositoryPath)
		{
			using (var repo = new Repository(repositoryPath))
			{
				return repo.Head.Tip.Sha;
			}
		}
	}
}