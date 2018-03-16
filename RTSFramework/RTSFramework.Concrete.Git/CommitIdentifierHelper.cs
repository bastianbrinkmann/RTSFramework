using System;
using System.IO;
using LibGit2Sharp;
using RTSFramework.Concrete.Git.Artefacts;

namespace RTSFramework.Concrete.Git
{
    public static class CommitIdentifierHelper
    {
        public static string GetVersionIdentifier(string repositoryPath, VersionReferenceType referenceType)
        {
            using (Repository repo = new Repository(repositoryPath))
            {
                if (referenceType == VersionReferenceType.LatestCommit)
                {
                    return $"GitRepo_{Path.GetFileName(repositoryPath)}_{repo.Head.Tip.Id.Sha}";
                }
                if (referenceType == VersionReferenceType.CurrentChanges)
                {
                    return $"Uncommitted_Changes_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";
                }
            }

            return null;
        }
    }
}