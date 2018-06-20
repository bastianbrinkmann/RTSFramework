using System.IO;
using System.Linq;
using Microsoft.Msagl.Core.Geometry.Curves;
using RTSFramework.Concrete.Reporting;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.SecondaryFeature;

namespace RTSFramework.Core.StatisticsReporting
{
	public class AveragePercentageImpactedTestsReporter : IStatisticsReporter
	{
		private readonly IArtefactAdapter<CsvFileArtefact, PercentageImpactedTestsStatistic> artefactAdapter;

		public AveragePercentageImpactedTestsReporter(IArtefactAdapter<CsvFileArtefact, PercentageImpactedTestsStatistic> artefactAdapter)
		{
			this.artefactAdapter = artefactAdapter;
		}

		public StatisticsReportData GetStatisticsReport()
		{
			var statistics = artefactAdapter.Parse(new CsvFileArtefact {CsvFilePath = PercentageImpactedTestsStatisticCsvFileAdapter.StatisticsFilePath});
			double totalAmountPercentages = statistics.DeltaIdPercentageTestsTuples.Sum(x => x.Item2);

			double averagePercentage = totalAmountPercentages / statistics.DeltaIdPercentageTestsTuples.Count;

			var report = new StatisticsReportData();
			report.ReportData.Add($"Total amount of analyzed runs: {statistics.DeltaIdPercentageTestsTuples.Count}");
			report.ReportData.Add($"Average Percentage of impacted tests: {averagePercentage}");

			return report;
		}
	}
}