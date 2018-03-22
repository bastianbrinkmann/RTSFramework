using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core.Artefacts;

namespace RTSFramework.Concrete.Git.Artefacts
{
    public class GitProgramModel : IProgramModel
    {
        public GitVersionReferenceType GitVersionReferenceType { get; set; }
        public string VersionId { get; set; }
        public string CommitId { get; set; }
        public string RepositoryPath { get; set; }
    }
}