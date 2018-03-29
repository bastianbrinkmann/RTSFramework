using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.Adapter
{
    public interface IDeltaAdapter<TPeFrom, TPeTo> where TPeFrom : IProgramModelElement where TPeTo : IProgramModelElement
    {
        StructuralDelta<TPeTo> Convert(StructuralDelta<TPeFrom> delta);
    }
}