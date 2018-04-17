using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
	public interface IExecutionWithCodeCoverageResult
	{
		CoverageData CoverageData { get; }
	}
}