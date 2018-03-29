using System.Collections.Generic;

namespace RTSFramework.Contracts.Models
{
    public class CoverageData
    {
        public CoverageData(HashSet<CoverageDataEntry> coverageDataEntries)
        {
            CoverageDataEntries = coverageDataEntries;
        }

        public HashSet<CoverageDataEntry> CoverageDataEntries { get; }
    }
}