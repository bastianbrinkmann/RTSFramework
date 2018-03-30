using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.Git.Models
{
    public class GitProgramModel : ICSharpProgramModel
    {
        public GitVersionReferenceType GitVersionReferenceType { get; set; }
        public string VersionId { get; set; }
        public string CommitId { get; set; }
        public string RepositoryPath { get; set; }
        public string AbsoluteSolutionPath { get; set; }
    }
}