using RTSFramework.Contracts.Models;

namespace RTSFramework.RTSApproaches.Dynamic
{
	public interface IDynamicRTSApproach
	{
		void UpdateCorrespondenceModel(CoverageData coverageData);
	}
}