using RTSFramework.Contracts.Adapter;
using RTSFramework.Core.Models;

namespace RTSFramework.Concrete.TFS2010
{
    public class TFS2010ProgramModelAdapter : IArtefactAdapter<TFS2010VersionIdentification, FilesProgramModel>
    {
	    public FilesProgramModel Parse(TFS2010VersionIdentification artefact)
	    {
		    return new FilesProgramModel
			{
			    AbsoluteSolutionPath = artefact.AbsoluteSolutionPath,
			    VersionId = artefact.CommitId
		    };
	    }

	    public TFS2010VersionIdentification Unparse(FilesProgramModel model, TFS2010VersionIdentification artefact)
	    {
		    throw new System.NotImplementedException();
	    }
    }
}