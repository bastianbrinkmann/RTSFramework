using System;
using System.IO;
using LibGit2Sharp;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Contracts.Adapter;

namespace RTSFramework.Concrete.Git
{
    public class GitProgramModelAdapter : IArtefactAdapter<GitVersionIdentification, GitProgramModel>
    {
	    private readonly GitCommitsProvider gitCommitsProvider;

	    public GitProgramModelAdapter(GitCommitsProvider gitCommitsProvider)
	    {
		    this.gitCommitsProvider = gitCommitsProvider;
	    }

	    public GitProgramModel Parse(GitVersionIdentification artefact)
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
			return new GitProgramModel
			{
				VersionId = gitProgramModelId,
				GranularityLevel = artefact.GranularityLevel,
				AbsoluteSolutionPath = artefact.AbsoluteSolutionPath,
				GitVersionIdentification = artefact
			};
		}

	    public void Unparse(GitProgramModel model, GitVersionIdentification artefact)
	    {
		    throw new NotImplementedException();
	    }
    }
}