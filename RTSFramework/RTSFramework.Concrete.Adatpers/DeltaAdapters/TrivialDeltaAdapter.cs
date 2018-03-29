using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Concrete.Adatpers.DeltaAdapters
{
    public class TrivialDeltaAdapter<TP, TPe> : IDeltaAdapter<TPe, TPe> where TPe : IProgramModelElement
    {
        public StructuralDelta<TP, TPe> Convert(StructuralDelta<TP, TPe> delta)
        {
            return delta;
        }
    }
}