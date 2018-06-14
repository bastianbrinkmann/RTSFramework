using System.IO;
using RTSFramework.Concrete.Reporting;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Core
{
	public class StatisticsReportDataStringAdapter : IArtefactAdapter<string, StatisticsReportData>
	{
		public StatisticsReportData Parse(string artefact)
		{
			throw new System.NotImplementedException();
		}

		public string Unparse(StatisticsReportData model, string artefact = null)
		{
			return string.Join("\n", model.ReportData);
		}
	}
}