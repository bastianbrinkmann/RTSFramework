using System.IO;
using Microsoft.Msagl.Core.Geometry.Curves;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.SecondaryFeature;

namespace RTSFramework.Core.StatisticsReporting
{
	public class AveragePercentageImpactedTestsReporter : IStatisticsReporter
	{
		public StatisticsReportData GetStatisticsReport()
		{
			var statistics = File.ReadAllLines(PercentageImpactedTestsStatisticCsvFileAdapter.StatisticsFilePath);
			double totalAmountPercentages = 0;

			foreach (var line in statistics)
			{
				var columns = line.Split(';');
				double percentage = double.Parse(columns[1]);

				totalAmountPercentages += percentage;
			}

			double averagePercentage = totalAmountPercentages / statistics.Length;

			var report = new StatisticsReportData();
			report.ReportData.Add($"Total amount of analyzed runs: {statistics.Length}");
			report.ReportData.Add($"Average Percentage of impacted tests: {averagePercentage}");

			return report;
		}
	}
}