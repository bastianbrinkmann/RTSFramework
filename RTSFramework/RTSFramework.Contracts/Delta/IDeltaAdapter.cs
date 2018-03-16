using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts.Delta
{
    public interface IDeltaAdapter<TPeFrom, TPeTo> where TPeFrom : IProgramModelElement where TPeTo : IProgramModelElement
    {
        StructuralDelta<TPeTo> Convert(StructuralDelta<TPeFrom> delta);
    }
}