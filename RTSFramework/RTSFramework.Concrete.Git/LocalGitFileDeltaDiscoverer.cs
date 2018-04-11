using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;
using RTSFramework.Core.Utilities;

namespace RTSFramework.Concrete.Git
{
    public class LocalGitFileDeltaDiscoverer : IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>
    {
		public StructuralDelta<GitProgramModel, FileElement> Discover(GitProgramModel oldModel, GitProgramModel newModel)
        {
            if (oldModel.RepositoryPath != newModel.RepositoryPath)
            {
                throw new ArgumentException($"Git Models must be for the same repository! OldRepoPath: {oldModel.RepositoryPath} NewRepoPath: {newModel.RepositoryPath}");
            }

            var repositoryPath = oldModel.RepositoryPath;

            var delta = new StructuralDelta<GitProgramModel, FileElement>
			{
                SourceModel = oldModel,
                TargetModel = newModel
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
                        var relativePathToSolution = RelativePathHelper.GetRelativePath(newModel, fullPath);

                        switch (change.Status)
                        {
                            case ChangeKind.Added:
                                if (delta.AddedElements.All(x => !x.Id.Equals(relativePathToSolution, StringComparison.Ordinal)))
                                    delta.AddedElements.Add(new FileElement(relativePathToSolution));
                                break;
                            case ChangeKind.Deleted:
                                if (delta.DeletedElements.All(x => !x.Id.Equals(relativePathToSolution, StringComparison.Ordinal)))
                                    delta.DeletedElements.Add(new FileElement(relativePathToSolution));
                                break;
                            case ChangeKind.Modified:
                                if (delta.ChangedElements.All(x => !x.Id.Equals(relativePathToSolution, StringComparison.Ordinal)))
                                    delta.ChangedElements.Add(new FileElement(relativePathToSolution));
                                break;
                        }
                    }
                }
            }

            return delta;
        }
    }
}