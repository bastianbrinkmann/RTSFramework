using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using RTSFramework.Concrete.Git.Artefacts;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Delta;
using RTSFramework.Core.Artefacts;

namespace RTSFramework.Concrete.Git
{
    //TODO This is more a Delta provider than a discoverer?
    public class LocalGitChangedFilesDiscoverer : IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>
    {
        //TODO: Instead of CSharpFileElement return string and use DeltaAdapter to filter only the CSharpDocuments
        public StructuralDelta<FileElement> Discover(GitProgramModel oldModel, GitProgramModel newModel)
        {
            //TODO: Console Read for RepositoryPath
            if (oldModel.RepositoryPath != newModel.RepositoryPath)
            {
                throw new ArgumentException($"Git Models must be for the same repository! OldRepoPath: {oldModel.RepositoryPath} NewRepoPath: {newModel.RepositoryPath}");
            }

            var repositoryPath = oldModel.RepositoryPath;

            var delta = new StructuralDelta<FileElement>
            {
                SourceModelId = oldModel.VersionId,
                TargetModelId = newModel.VersionId
            };

            using (Repository repo = new Repository(repositoryPath))
            {
                if (oldModel.GitVersionReferenceType == GitVersionReferenceType.LatestCommit &&
                    newModel.GitVersionReferenceType == GitVersionReferenceType.CurrentChanges)
                {
                    TreeChanges changes = repo.Diff.Compare<TreeChanges>();
                    foreach (TreeEntryChanges change in changes)
                    {
                        var filePath = change.Path;
                        var fullPath = Path.Combine(repositoryPath, filePath);

                        switch (change.Status)
                        {
                            case ChangeKind.Added:
                                if (delta.AddedElements.All(x => !x.Id.Equals(fullPath, StringComparison.Ordinal)))
                                    delta.AddedElements.Add(new FileElement(fullPath));
                                break;
                            case ChangeKind.Deleted:
                                if (delta.DeletedElements.All(x => !x.Id.Equals(fullPath, StringComparison.Ordinal)))
                                    delta.DeletedElements.Add(new FileElement(fullPath));
                                break;
                            case ChangeKind.Modified:
                                if (delta.ChangedElements.All(x => !x.Id.Equals(fullPath, StringComparison.Ordinal)))
                                    delta.ChangedElements.Add(new FileElement(fullPath));
                                break;
                        }
                    }
                }
            }

            return delta;
        }
    }
}