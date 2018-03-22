using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Core
{
    public class CoverageData : ICoverageData
    {
        public Dictionary<string, HashSet<string>> TestCaseToProgramElementsMap { get; set; } 
            = new Dictionary<string, HashSet<string>>();
    }
}