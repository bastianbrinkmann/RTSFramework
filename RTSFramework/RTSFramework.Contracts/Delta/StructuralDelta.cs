﻿using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts.Delta
{
    public class StructuralDelta<TPe> : IDelta where TPe : IProgramModelElement
    {
        public List<TPe> AddedElements { get; } = new List<TPe>();

        public List<TPe> DeletedElements { get; } = new List<TPe>();

        public List<TPe> ChangedElements { get; } = new List<TPe>();

        public string SourceModelId { get; set; }

        public string TargetModelId { get; set; }
    }
}