using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Core
{
    public class CoverageData : ICoverageData
    {
        public Dictionary<string, HashSet<string>> TransitiveClosureTestsToProgramElements { get; set; } 
            = new Dictionary<string, HashSet<string>>();
    }
}