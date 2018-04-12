using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts.Adapter
{
    public interface IArtefactAdapter<TArtefact, TModel>
    {
        TModel Parse(TArtefact artefact);

        void Unparse(TModel model, TArtefact artefact);
    }
}