using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Core.Utilities;

namespace RTSFramework.Concrete.Git
{
    public class GitFilesProvider : IFilesProvider
    {
        public string GetFileContent(IProgramModel programModel, string path)
        {
	        var gitProgramModel = programModel as GitProgramModel;

	        if (gitProgramModel == null)
	        {
		        throw new ArgumentException("Must be git program Model");
	        }

            using (var repo = new Repository(gitProgramModel.RepositoryPath))
            {
                var commit = repo.Lookup<Commit>(gitProgramModel.CommitId);

                string relPath;
                Uri fullUri, relRoot = new Uri(gitProgramModel.RootPath, UriKind.Absolute);
                if (Uri.TryCreate(path, UriKind.Absolute, out fullUri))
                {
                    relPath = relRoot.MakeRelativeUri(fullUri).ToString();
                }
                else
                {
                    var absolutePath = RelativePathHelper.GetAbsolutePath(programModel, path);
                    relPath = relRoot.MakeRelativeUri(new Uri(absolutePath, UriKind.Absolute)).ToString();
                }

                var treeEntry = commit[relPath];

                var blob = (Blob)treeEntry.Target;
                var contentStream = blob.GetContentStream();

                using (var tr = new StreamReader(contentStream, Encoding.UTF8))
                {
                    return tr.ReadToEnd();
                }
            }
        }

        public List<string> GetAllFiles(IProgramModel programModel)
        {
			var gitProgramModel = programModel as GitProgramModel;

			if (gitProgramModel == null)
			{
				throw new ArgumentException("Must be git program Model");
			}

			using (var repo = new Repository(gitProgramModel.RepositoryPath))
            {
                var commit = repo.Lookup<Commit>(gitProgramModel.CommitId);

                return commit.Tree.Select(x => Path.Combine(gitProgramModel.RepositoryPath, x.Path)).ToList();
            }
        }
    }
}