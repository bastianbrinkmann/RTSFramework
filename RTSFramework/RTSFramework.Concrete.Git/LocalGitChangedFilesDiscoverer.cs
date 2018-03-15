using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.Git.Artefacts;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core;

namespace RTSFramework.Concrete.Git
{
    public class LocalGitChangedFilesDiscoverer : IOfflineDeltaDiscoverer<GitProgramVersion, CSharpDocument, StructuralDelta<CSharpDocument, GitProgramVersion>>
    {
        private readonly string repositoryPath;

        public LocalGitChangedFilesDiscoverer(string repositoryPath)
        {
            this.repositoryPath = repositoryPath;
        }

        //TODO: Instead of CSharpDocument return string and use DeltaAdapter to filter only the CSharpDocuments
        public StructuralDelta<CSharpDocument, GitProgramVersion> Discover(GitProgramVersion oldVersion, GitProgramVersion newVersion)
        {
            var delta = new StructuralDelta<CSharpDocument, GitProgramVersion>
            {
                Source = oldVersion
            };

            using (Repository repo = new Repository(repositoryPath))
            {
                if (oldVersion.VersionReferenceType == VersionReferenceType.LatestCommit)
                {
                    oldVersion.VersionId = $"GitRepo_{Path.GetFileName(repositoryPath)}_{repo.Head.Tip.Id.Sha}";
                }
                if (newVersion.VersionReferenceType == VersionReferenceType.CurrentChanges)
                {
                    newVersion.VersionId = $"Uncommitted_Changes_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";
                }

                if (oldVersion.VersionReferenceType == VersionReferenceType.LatestCommit &&
                    newVersion.VersionReferenceType == VersionReferenceType.CurrentChanges)
                {
                    TreeChanges changes = repo.Diff.Compare<TreeChanges>();
                    foreach (TreeEntryChanges change in changes)
                    {
                        var filePath = change.Path;
                        var fullPath = Path.Combine(repositoryPath, filePath);

                        if (fullPath.EndsWith(".cs"))
                        {
                            switch (change.Status)
                            {
                                case ChangeKind.Added:
                                    if(delta.AddedElements.All(x => !x.Id.Equals(fullPath, StringComparison.Ordinal)))
                                        delta.AddedElements.Add(new CSharpDocument(fullPath));
                                    break;
                                case ChangeKind.Deleted:
                                    if (delta.DeletedElements.All(x => !x.Id.Equals(fullPath, StringComparison.Ordinal)))
                                        delta.DeletedElements.Add(new CSharpDocument(fullPath));
                                    break;
                                case ChangeKind.Modified:
                                    if (delta.ChangedElements.All(x => !x.Id.Equals(fullPath, StringComparison.Ordinal)))
                                        delta.ChangedElements.Add(new CSharpDocument(fullPath));
                                    break;
                            }
                        }
                    }

                }
            }

            return delta;
        }
    }
}