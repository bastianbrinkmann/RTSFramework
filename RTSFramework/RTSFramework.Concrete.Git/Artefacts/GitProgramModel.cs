using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core.Artefacts;

namespace RTSFramework.Concrete.Git.Artefacts
{
    public class GitProgramModel : IProgramModel
    {
        public VersionReferenceType VersionReferenceType { get; set; }
        public string VersionId { get; set; }
    }
}