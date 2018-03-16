using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.Git.Artefacts;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.Adatpers.ArtefactAdapters
{
    public class GitProgramCSharpProgramAdapter : IArtefactAdapter<GitProgramModel, CSharpProgramModel>
    {
        public CSharpProgramModel Convert(GitProgramModel model)
        {
            return new CSharpProgramModel
            {
                VersionId = model.VersionId
            };
        }
    }
}