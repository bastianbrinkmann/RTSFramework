using RTSFramework.Concrete.TFS2010.Models;
using RTSFramework.Contracts.Adapter;

namespace RTSFramework.Concrete.TFS2010
{
    public class TFS2010ProgramModelAdapter : IArtefactAdapter<TFS2010VersionIdentification, TFS2010ProgramModel>
    {
	    public TFS2010ProgramModel Parse(TFS2010VersionIdentification artefact)
	    {
		    return new TFS2010ProgramModel
		    {
			    AbsoluteSolutionPath = artefact.AbsoluteSolutionPath,
			    GranularityLevel = artefact.GranularityLevel,
			    VersionId = artefact.CommitId
		    };
	    }

	    public TFS2010VersionIdentification Unparse(TFS2010ProgramModel model, TFS2010VersionIdentification artefact)
	    {
		    throw new System.NotImplementedException();
	    }
    }
}