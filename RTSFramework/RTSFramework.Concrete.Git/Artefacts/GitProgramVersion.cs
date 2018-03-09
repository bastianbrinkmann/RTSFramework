using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.Git.Artefacts
{
    public class GitProgramVersion : IProgram
    {
        public VersionReferenceType VersionReferenceType { get; }

        public string CommitIdentifier { get; }

        public GitProgramVersion(VersionReferenceType referenceType, string commitIdentifier = null)
        {
            VersionReferenceType = referenceType;
            CommitIdentifier = commitIdentifier;
        }
    }
}