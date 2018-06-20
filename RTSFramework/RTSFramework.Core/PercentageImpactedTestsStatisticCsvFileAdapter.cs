using System;
using System.IO;
using RTSFramework.Concrete.Reporting;
using RTSFramework.Contracts.Adapter;

namespace RTSFramework.Core
{
	public class PercentageImpactedTestsStatisticCsvFileAdapter : IArtefactAdapter<CsvFileArtefact, PercentageImpactedTestsStatistic>
	{
		public const string StatisticsFilePath = "PercentageImpactedTestsStatistics.csv";

		public PercentageImpactedTestsStatistic Parse(CsvFileArtefact artefact)
		{
			var persistedStatistics = File.ReadAllLines(StatisticsFilePath);
			var statistics = new PercentageImpactedTestsStatistic();

			foreach (var line in persistedStatistics)
			{
				var columns = line.Split(';');
				statistics.DeltaIdPercentageTestsTuples.Add(new Tuple<string, double>(columns[0], double.Parse(columns[1])));
			}

			return statistics;
		}

		public CsvFileArtefact Unparse(PercentageImpactedTestsStatistic model, CsvFileArtefact artefact = null)
		{
			foreach (var tuple in model.DeltaIdPercentageTestsTuples)
			{
				File.AppendAllText(Path.GetFullPath(StatisticsFilePath), $"{tuple.Item1};{tuple.Item2}\n");
			}

			return new CsvFileArtefact {CsvFilePath = StatisticsFilePath};
		}
	}
}