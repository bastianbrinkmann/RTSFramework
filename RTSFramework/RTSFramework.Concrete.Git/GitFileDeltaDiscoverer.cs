using System;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;
using RTSFramework.Core.Utilities;
using Unity.Interception.Utilities;

namespace RTSFramework.Concrete.Git
{
    public class GitFileDeltaDiscoverer : IOfflineDeltaDiscoverer<GitCSharpProgramModel, StructuralDelta<GitCSharpProgramModel, FileElement>>
    {
		public StructuralDelta<GitCSharpProgramModel, FileElement> Discover(GitCSharpProgramModel oldModel, GitCSharpProgramModel newModel)
        {
            if (oldModel.GitVersionIdentification.RepositoryPath != newModel.GitVersionIdentification.RepositoryPath)
            {
                throw new ArgumentException($"Git Models must be for the same repository! OldRepoPath: {oldModel.GitVersionIdentification.RepositoryPath} NewRepoPath: {newModel.GitVersionIdentification.RepositoryPath}");
            }

            var repositoryPath = oldModel.GitVersionIdentification.RepositoryPath;

	        var delta = new StructuralDelta<GitCSharpProgramModel, FileElement>(oldModel, newModel);

            using (Repository repo = new Repository(repositoryPath))
            {
				AddDeltaBetweenCommits(delta, repo, oldModel.GitVersionIdentification, newModel.GitVersionIdentification, repositoryPath);

                if (newModel.GitVersionIdentification.ReferenceType == GitVersionReferenceType.CurrentChanges)
                {
	                AddCurrentChanges(delta, repo);
                }
            }

	        return delta;
        }

	    private void AddCurrentChanges(StructuralDelta<GitCSharpProgramModel, FileElement> delta, Repository repo)
	    {
		    var lastCommit = repo.Head.Tip;

			var status = repo.RetrieveStatus();
			status.Added.Union(status.Untracked).ForEach(addedFile =>
			{
				var fullPath = Path.Combine(delta.NewModel.GitVersionIdentification.RepositoryPath, addedFile.FilePath);
				var relativePathToSolution = RelativePathHelper.GetRelativePath(delta.NewModel, fullPath);

				if (delta.AddedElements.All(x => !x.Id.Equals(relativePathToSolution, StringComparison.Ordinal)))
					delta.AddedElements.Add(new FileElement(relativePathToSolution, () => File.ReadAllText(fullPath)));
			});

			status.Modified.Union(status.Staged).ForEach(changedFile =>
			{
				var fullPath = Path.Combine(delta.NewModel.GitVersionIdentification.RepositoryPath, changedFile.FilePath);
				var relativePathToSolution = RelativePathHelper.GetRelativePath(delta.NewModel, fullPath);

				if (delta.ChangedElements.All(x => !x.Id.Equals(relativePathToSolution, StringComparison.Ordinal)))
					delta.ChangedElements.Add(new FileElement(relativePathToSolution, () => GetContent(delta.NewModel.GitVersionIdentification.RepositoryPath, lastCommit.Id.Sha, changedFile.FilePath)));
			});

			status.Missing.Union(status.Removed).ForEach(changedFile =>
			{
				var fullPath = Path.Combine(delta.NewModel.GitVersionIdentification.RepositoryPath, changedFile.FilePath);
				var relativePathToSolution = RelativePathHelper.GetRelativePath(delta.NewModel, fullPath);

				if (delta.DeletedElements.All(x => !x.Id.Equals(relativePathToSolution, StringComparison.Ordinal)))
					delta.DeletedElements.Add(new FileElement(relativePathToSolution, () => GetContent(delta.NewModel.GitVersionIdentification.RepositoryPath, lastCommit.Id.Sha, changedFile.FilePath)));
			});
		}

	    private void AddDeltaBetweenCommits(StructuralDelta<GitCSharpProgramModel, FileElement> delta, Repository repo, GitVersionIdentification oldVersion, GitVersionIdentification newVersion, string repositoryPath)
	    {
		    bool collectDelta = newVersion.ReferenceType == GitVersionReferenceType.CurrentChanges;

		    for(int i = 0; i < repo.Commits.Count(); i++)
		    {
			    var commit = repo.Commits.ElementAt(i);

				if (commit.Id.Sha == newVersion.Commit.ShaId)
				{
					collectDelta = true;
				}

				if (commit.Id.Sha == oldVersion.Commit.ShaId)
				{
					break;
				}

				if (collectDelta)
				{
					if (i + 1 < repo.Commits.Count())
					{
						var previousCommit = repo.Commits.ElementAt(i + 1);
						TreeChanges diff = repo.Diff.Compare<TreeChanges>(previousCommit.Tree, commit.Tree);

						AddDeltaInTreeChanges(delta, diff, previousCommit, repositoryPath, commit);
					}
					else
					{
						foreach (TreeEntry entry in commit.Tree)
						{
							var filePath = entry.Path;
							var fullPath = Path.Combine(delta.NewModel.GitVersionIdentification.RepositoryPath, filePath);
							var relativePathToSolution = RelativePathHelper.GetRelativePath(delta.NewModel, fullPath);

							if (delta.AddedElements.All(x => !x.Id.Equals(relativePathToSolution, StringComparison.Ordinal)))
								delta.AddedElements.Add(new FileElement(relativePathToSolution, () => GetContent(repositoryPath, commit.Id.Sha, filePath)));
						}
					}
				}
		    }
	    }

	    private string GetContent(string repositoryPath, string commitId, string relPath)
	    {
		    using (var repo = new Repository(repositoryPath))
		    {
			    var commit = repo.Lookup<Commit>(commitId);

				var treeEntry = commit[relPath];

				var blob = (Blob)treeEntry.Target;
				var contentStream = blob.GetContentStream();

				using (var tr = new StreamReader(contentStream, Encoding.UTF8))
				{
					return tr.ReadToEnd();
				}
			}
		}

	    private void AddDeltaInTreeChanges(StructuralDelta<GitCSharpProgramModel, FileElement> delta, TreeChanges treeChanges, Commit previousCommit, string repositoryPath, Commit commit = null)
	    {
			foreach (TreeEntryChanges change in treeChanges)
			{
				var filePath = change.Path;
				var fullPath = Path.Combine(delta.NewModel.GitVersionIdentification.RepositoryPath, filePath);
				var relativePathToSolution = RelativePathHelper.GetRelativePath(delta.NewModel, fullPath);

				switch (change.Status)
				{
					case ChangeKind.Added:
						if (delta.AddedElements.All(x => !x.Id.Equals(relativePathToSolution, StringComparison.Ordinal)))
							delta.AddedElements.Add(new FileElement(relativePathToSolution, () =>
							{
								if (commit == null)
								{
									return File.ReadAllText(fullPath);
								}
								return GetContent(repositoryPath, commit.Id.Sha, filePath);
							}));
						break;
					case ChangeKind.Deleted:
						if (delta.DeletedElements.All(x => !x.Id.Equals(relativePathToSolution, StringComparison.Ordinal)))
							delta.DeletedElements.Add(new FileElement(relativePathToSolution, () => GetContent(repositoryPath, previousCommit.Id.Sha, filePath)));
						break;
					case ChangeKind.Modified:
						if (delta.ChangedElements.All(x => !x.Id.Equals(relativePathToSolution, StringComparison.Ordinal)))
							delta.ChangedElements.Add(new FileElement(relativePathToSolution, () => GetContent(repositoryPath, previousCommit.Id.Sha, filePath)));
						break;
				}
			}
		}
    }
}