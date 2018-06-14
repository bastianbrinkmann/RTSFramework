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
			throw new System.NotImplementedException();
		}

		public CsvFileArtefact Unparse(PercentageImpactedTestsStatistic model, CsvFileArtefact artefact = null)
		{
			File.AppendAllText(Path.GetFullPath(StatisticsFilePath), $"{model.DeltaIdentifier};{model.PercentageImpactedTests}\n");
			return new CsvFileArtefact {CsvFilePath = StatisticsFilePath};
		}
	}
}