using System.Collections.Generic;

namespace RTSFramework.Contracts.Models
{
    public interface ICoverageData
    {
        Dictionary<string, HashSet<string>> TransitiveClosureTestsToProgramElements { get; set; }
    }
}