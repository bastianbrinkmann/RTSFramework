﻿using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Core
{
    public class OperationalDelta<TPe> : IDelta<TPe> where TPe : IProgramElement
    {
        public IList<TPe> AddedElements { get; } = new List<TPe>();

        public IList<TPe> RemovedElements { get; } = new List<TPe>();

        public IList<TPe> ChangedElements { get; } = new List<TPe>();
    }
}