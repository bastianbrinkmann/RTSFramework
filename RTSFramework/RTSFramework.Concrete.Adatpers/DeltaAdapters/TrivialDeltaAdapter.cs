using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Concrete.Adatpers.DeltaAdapters
{
    public class TrivialDeltaAdapter<TPe> : IDeltaAdapter<TPe, TPe> where TPe : IProgramModelElement
    {
        public StructuralDelta<TPe> Convert(StructuralDelta<TPe> delta)
        {
            return delta;
        }
    }
}