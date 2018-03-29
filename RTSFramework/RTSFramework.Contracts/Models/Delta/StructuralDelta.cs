using System.Collections.Generic;

namespace RTSFramework.Contracts.Models.Delta
{
    public class StructuralDelta<TP, TPe> : IDelta<TP> where TPe : IProgramModelElement where TP : IProgramModel
    {
        public List<TPe> AddedElements { get; } = new List<TPe>();

        public List<TPe> DeletedElements { get; } = new List<TPe>();

        public List<TPe> ChangedElements { get; } = new List<TPe>();
        public TP SourceModel { get; set; }
        public TP TargetModel { get; set; }
    }
}