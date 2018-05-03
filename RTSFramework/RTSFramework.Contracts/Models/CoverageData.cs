using System;
using System.Collections.Generic;

namespace RTSFramework.Contracts.Models
{
    public class CoverageData
    {
        public CoverageData(HashSet<Tuple<string, string>> coverageDataEntries)
        {
            CoverageDataEntries = coverageDataEntries;
        }

        public HashSet<Tuple<string, string>> CoverageDataEntries { get; }
    }
}