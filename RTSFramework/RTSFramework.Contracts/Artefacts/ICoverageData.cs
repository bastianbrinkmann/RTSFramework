using System.Collections.Generic;

namespace RTSFramework.Contracts.Artefacts
{
    public interface ICoverageData
    {
        Dictionary<string, HashSet<string>> TestCaseToProgramElementsMap { get; set; }
    }
}