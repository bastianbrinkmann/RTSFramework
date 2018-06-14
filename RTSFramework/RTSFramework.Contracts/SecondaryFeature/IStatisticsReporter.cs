using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts.SecondaryFeature
{
	public interface IStatisticsReporter
	{
		StatisticsReportData GetStatisticsReport();
	}
}