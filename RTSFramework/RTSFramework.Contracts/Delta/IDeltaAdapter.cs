using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts.Delta
{
    public interface IDeltaAdapter<TPe1, TPe2> where TPe1 : IProgramModelElement where TPe2 : IProgramModelElement
    {
        StructuralDelta<TPe1> Convert(StructuralDelta<TPe2> delta);

        StructuralDelta<TPe2> Convert(StructuralDelta<TPe1> delta);
    }
}