namespace RTSFramework.Contracts.Artefacts
{
    public interface IArtefactAdapter<TPFrom, TPTo> where TPFrom : IProgramModel where TPTo : IProgramModel
    {
        TPTo Convert(TPFrom model);
    }
}