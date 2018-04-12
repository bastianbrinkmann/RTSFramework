﻿using System;
using System.IO;
using LibGit2Sharp;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Contracts.Adapter;

namespace RTSFramework.Concrete.Git
{
    public class GitProgramModelAdapter : IArtefactAdapter<GitVersionIdentification, GitProgramModel>
    {
	    public GitProgramModel Parse(GitVersionIdentification artefact)
	    {
		    string gitProgramModelId = null;

			using (Repository repo = new Repository(artefact.RepositoryPath))
			{
				switch (artefact.ReferenceType)
				{
					case GitVersionReferenceType.LatestCommit:
						artefact.Commit.ShaId = repo.Head.Tip.Id.Sha;
						gitProgramModelId = $"GitRepo_{new DirectoryInfo(artefact.RepositoryPath).Name}_{artefact.Commit.ShaId}";
						break;
					case GitVersionReferenceType.CurrentChanges:
						gitProgramModelId = $"Uncommitted_Changes_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";
						break;
					case GitVersionReferenceType.SpecificCommit:
						gitProgramModelId = $"GitRepo_{new DirectoryInfo(artefact.RepositoryPath).Name}_{artefact.Commit.ShaId}";
						break;
				}
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