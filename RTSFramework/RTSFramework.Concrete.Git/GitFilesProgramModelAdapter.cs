using System;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;
using RTSFramework.Core.Utilities;
using Unity.Interception.Utilities;

namespace RTSFramework.Concrete.Git
{
	public class GitFilesProgramModelAdapter : IArtefactAdapter<GitVersionIdentification, FilesProgramModel>
	{
		private readonly GitCommitsProvider gitCommitsProvider;

		public GitFilesProgramModelAdapter(GitCommitsProvider gitCommitsProvider)
		{
			this.gitCommitsProvider = gitCommitsProvider;
		}

		public FilesProgramModel Parse(GitVersionIdentification artefact)
		{
			string gitProgramModelId = null;

			if (!Repository.IsValid(artefact.RepositoryPath))
			{
				throw new ArgumentException($"There is no valid Git repository at '{artefact.RepositoryPath}'.");
			}

			switch (artefact.ReferenceType)
			{
				case GitVersionReferenceType.CurrentChanges:
					gitProgramModelId = $"Uncommitted_Changes_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";
					break;
				case GitVersionReferenceType.SpecificCommit:
					gitProgramModelId = gitCommitsProvider.GetCommitIdentifier(artefact.RepositoryPath, artefact.Commit.ShaId);
					break;
			}

			var model = new FilesProgramModel
			{
				VersionId = gitProgramModelId,
				AbsoluteSolutionPath = artefact.AbsoluteSolutionPath
			};

			using (Repository repo = new Repository(artefact.RepositoryPath))
			{
				if (artefact.ReferenceType == GitVersionReferenceType.CurrentChanges)
				{
					AddCurrentChanges(artefact, model, repo);
				}
				else
				{
					AddFilesAtCommit(artefact, model, repo);
				}
			}

			return model;
		}

		private void AddFilesAtCommit(GitVersionIdentification versionIdentification, FilesProgramModel model, Repository repo)
		{
			var commit = repo.Commits.SingleOrDefault(x => x.Id.Sha == versionIdentification.Commit.ShaId);
			
			if (commit != null)
			{
				TreeChanges diff = repo.Diff.Compare<TreeChanges>(default(Tree), commit.Tree);

				foreach (var entry in diff)
				{
					var filePath = entry.Path;
					var fullPath = Path.Combine(versionIdentification.RepositoryPath, filePath);
					var relativePathToSolution = RelativePathHelper.GetRelativePath(model, fullPath);

					if (model.Files.All(x => !x.Id.Equals(relativePathToSolution, StringComparison.Ordinal)))
						model.Files.Add(new FileElement(relativePathToSolution, () => GetContent(versionIdentification.RepositoryPath, commit.Id.Sha, filePath)));
				}
			}
		}

		private void AddCurrentChanges(GitVersionIdentification versionIdentification, FilesProgramModel model, Repository repo)
		{
			var lastCommit = repo.Head.Tip;
			TreeChanges diff = repo.Diff.Compare<TreeChanges>(default(Tree), lastCommit.Tree);

			foreach (var entry in diff)
			{
				var filePath = entry.Path;
				var fullPath = Path.Combine(versionIdentification.RepositoryPath, filePath);
				var relativePathToSolution = RelativePathHelper.GetRelativePath(model, fullPath);

				if (model.Files.All(x => !x.Id.Equals(relativePathToSolution, StringComparison.Ordinal)))
					model.Files.Add(new FileElement(relativePathToSolution, () => GetContent(versionIdentification.RepositoryPath, lastCommit.Id.Sha, filePath)));
			}
			
			var status = repo.RetrieveStatus();
			
			status.Added.Union(status.Untracked).ForEach(addedFile =>
			{
				var fullPath = Path.Combine(versionIdentification.RepositoryPath, addedFile.FilePath);
				var relativePathToSolution = RelativePathHelper.GetRelativePath(model, fullPath);

				if (model.Files.All(x => !x.Id.Equals(relativePathToSolution, StringComparison.Ordinal)))
					model.Files.Add(new FileElement(relativePathToSolution, () => File.ReadAllText(fullPath)));
			});

			status.Modified.Union(status.Staged).ForEach(changedFile =>
			{
				var fullPath = Path.Combine(versionIdentification.RepositoryPath, changedFile.FilePath);
				var relativePathToSolution = RelativePathHelper.GetRelativePath(model, fullPath);

				var file = model.Files.SingleOrDefault(x => x.Id.Equals(relativePathToSolution, StringComparison.Ordinal));

				if (file != null)
				{
					model.Files.Remove(file);
					model.Files.Add(new FileElement(relativePathToSolution, () => File.ReadAllText(fullPath)));
				}
			});

			status.Missing.Union(status.Removed).ForEach(changedFile =>
			{
				var fullPath = Path.Combine(versionIdentification.RepositoryPath, changedFile.FilePath);
				var relativePathToSolution = RelativePathHelper.GetRelativePath(model, fullPath);

				if (model.Files.Any(x => x.Id.Equals(relativePathToSolution, StringComparison.Ordinal)))
					model.Files.RemoveAll(x => x.Id.Equals(relativePathToSolution, StringComparison.Ordinal));
			});
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

		public GitVersionIdentification Unparse(FilesProgramModel model, GitVersionIdentification artefact)
		{
			throw new NotImplementedException();
		}
	}
}