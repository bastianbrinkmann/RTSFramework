namespace RTSFramework.Contracts.Artefacts
{
    public interface IArtefactAdapter<TArtefact, TModel>
    {
        TModel Parse(TArtefact artefact);

        void Unparse(TModel model, TArtefact artefact);
    }
}