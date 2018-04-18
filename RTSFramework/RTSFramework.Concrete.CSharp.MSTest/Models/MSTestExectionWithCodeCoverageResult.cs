using RTSFramework.Contracts.Models;
using RTSFramework.RTSApproaches.Dynamic;

namespace RTSFramework.Concrete.CSharp.MSTest.Models
{
    public class MSTestExectionWithCodeCoverageResult : MSTestExectionResult, IProcessingResultWithCodeCoverage
	{
	    public CoverageData CoverageData { get; set; }
    }
}