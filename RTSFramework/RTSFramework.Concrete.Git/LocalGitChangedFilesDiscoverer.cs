using System.Linq;
using LibGit2Sharp;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.Git.Artefacts;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core;

namespace RTSFramework.Concrete.Git
{
    public class LocalGitChangedFilesDiscoverer : IOfflineDeltaDiscoverer<GitProgramVersion, CSharpDocument, IDelta<CSharpDocument>>
    {
        private string repositoryPath;

        public LocalGitChangedFilesDiscoverer(string repositoryPath)
        {
            this.repositoryPath = repositoryPath;
        }

        //TODO: Instead of CSharpDocument return string and use DeltaAdapter to filter only the CSharpDocuments
        public IDelta<CSharpDocument> Discover(GitProgramVersion oldVersion, GitProgramVersion newVersion)
        {
            var delta = new OperationalDelta<CSharpDocument>();

            using (Repository repo = new Repository(repositoryPath))
            {
                if (oldVersion.VersionReferenceType == VersionReferenceType.LatestCommit &&
                    newVersion.VersionReferenceType == VersionReferenceType.CurrentChanges)
                {
                    TreeChanges changes = repo.Diff.Compare<TreeChanges>();
                    foreach (TreeEntryChanges change in changes)
                    {
                        var filePath = change.Path;
                        if (filePath.EndsWith(".cs"))
                        {
                            switch (change.Status)
                            {
                                case ChangeKind.Added:
                                    if(delta.AddedElements.All(x => x.Id != filePath))
                                        delta.AddedElements.Add(new CSharpDocument(filePath));
                                    break;
                                case ChangeKind.Deleted:
                                    if (delta.RemovedElements.All(x => x.Id != filePath))
                                        delta.RemovedElements.Add(new CSharpDocument(filePath));
                                    break;
                                case ChangeKind.Modified:
                                    if (delta.ChangedElements.All(x => x.Id != filePath))
                                        delta.ChangedElements.Add(new CSharpDocument(filePath));
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