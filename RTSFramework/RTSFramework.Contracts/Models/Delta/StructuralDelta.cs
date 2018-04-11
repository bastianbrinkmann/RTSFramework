using System.Collections.Generic;

namespace RTSFramework.Contracts.Models.Delta
{
    public class StructuralDelta : IDelta
    {
        public List<IProgramModelElement> AddedElements { get; } = new List<IProgramModelElement>();

        public List<IProgramModelElement> DeletedElements { get; } = new List<IProgramModelElement>();

        public List<IProgramModelElement> ChangedElements { get; } = new List<IProgramModelElement>();
        public IProgramModel SourceModel { get; set; }
        public IProgramModel TargetModel { get; set; }
    }
}