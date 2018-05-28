using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts.Adapter
{
    public interface IArtefactAdapter<TArtefact, TModel>
    {
        TModel Parse(TArtefact artefact);

		TArtefact Unparse(TModel model, TArtefact artefact = default(TArtefact));
    }
}