using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Core
{
    public class StructuralDelta<TPe, TP> : IDelta<TPe, TP> where TPe : IProgramElement where TP : IProgram
    {
        public IList<TPe> AddedElements { get; } = new List<TPe>();

        public IList<TPe> DeletedElements { get; } = new List<TPe>();

        public IList<TPe> ChangedElements { get; } = new List<TPe>();

        public TP Source { get; set; }
    }
}