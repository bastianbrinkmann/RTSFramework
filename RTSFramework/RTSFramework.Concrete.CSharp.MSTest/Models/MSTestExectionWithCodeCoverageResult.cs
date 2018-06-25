using RTSFramework.Contracts.Models;
using RTSFramework.RTSApproaches.Dynamic;

namespace RTSFramework.Concrete.CSharp.MSTest.Models
{
    public class MSTestExectionWithCodeCoverageResult : MSTestExectionResult, IProcessingResultWithCodeCoverage
	{
	    public CorrespondenceLinks CorrespondenceLinks { get; set; }
    }
}