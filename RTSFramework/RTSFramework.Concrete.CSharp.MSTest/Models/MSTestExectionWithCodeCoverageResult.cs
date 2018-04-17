using System.Collections.Generic;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.CSharp.MSTest.Models
{
    public class MSTestExectionWithCodeCoverageResult : MSTestExectionResult, IExecutionWithCodeCoverageResult
    {
	    public CoverageData CoverageData { get; set; }
    }
}