using System;
using System.IO;
using LibGit2Sharp;
using RTSFramework.Concrete.Git.Models;

namespace RTSFramework.Concrete.Git
{
    public static class GitProgramModelProvider
    {
        public static GitProgramModel GetGitProgramModel(string repositoryPath, GitVersionReferenceType referenceType)
        {
            string gitProgramModelId = null, commitid = null;

            using (Repository repo = new Repository(repositoryPath))
            {
                if (referenceType == GitVersionReferenceType.LatestCommit)
                {
                    commitid = repo.Head.Tip.Id.Sha;
                    gitProgramModelId = $"GitRepo_{Path.GetFileName(repositoryPath)}_{commitid}";
                }
                if (referenceType == GitVersionReferenceType.CurrentChanges)
                {
                    gitProgramModelId = $"Uncommitted_Changes_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";
                }
            }

            return new GitProgramModel
            {
                VersionId = gitProgramModelId,
                GitVersionReferenceType = referenceType,
                CommitId = commitid,
                RepositoryPath = repositoryPath
            };
        }
    }
}