using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.Git.Models
{
    public class GitProgramModel : IProgramModel
    {
        public GitVersionReferenceType GitVersionReferenceType { get; set; }
        public string VersionId { get; set; }
        public string CommitId { get; set; }
        public string RepositoryPath { get; set; }
    }
}