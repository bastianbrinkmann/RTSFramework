using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Contracts;

namespace RTSFramework.Concrete.Git
{
    public class GitFilesProvider : IFilesProvider<GitProgramModel>
    {
        public string GetFileContent(GitProgramModel programModel, string fullPath)
        {
            using (var repo = new Repository(programModel.RepositoryPath))
            {
                var commit = repo.Lookup<Commit>(programModel.CommitId);
                Uri fullUri = new Uri(fullPath, UriKind.Absolute);
                Uri relRoot = new Uri(programModel.RepositoryPath, UriKind.Absolute);

                string relPath = relRoot.MakeRelativeUri(fullUri).ToString();

                var treeEntry = commit[relPath];

                var blob = (Blob)treeEntry.Target;
                var contentStream = blob.GetContentStream();

                using (var tr = new StreamReader(contentStream, Encoding.UTF8))
                {
                    return tr.ReadToEnd();
                }
            }
        }

        public List<string> GetAllFiles(GitProgramModel programModel)
        {
            using (var repo = new Repository(programModel.RepositoryPath))
            {
                var commit = repo.Lookup<Commit>(programModel.CommitId);

                return commit.Tree.Select(x => Path.Combine(programModel.RepositoryPath, x.Path)).ToList();
            }
        }
    }
}