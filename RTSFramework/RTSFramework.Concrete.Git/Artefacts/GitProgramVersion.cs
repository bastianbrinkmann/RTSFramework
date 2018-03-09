using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.Git.Artefacts
{
    public class GitProgramVersion : IProgram
    {
        public VersionReferenceType VersionReferenceType { get; }

        public string VersionId { get; set; }

        public GitProgramVersion(VersionReferenceType referenceType, string versionId = null)
        {
            VersionReferenceType = referenceType;
            VersionId = versionId;
        }
    }
}