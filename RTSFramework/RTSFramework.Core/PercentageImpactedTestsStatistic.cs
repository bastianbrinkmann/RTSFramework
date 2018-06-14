using RTSFramework.Contracts;

namespace RTSFramework.Core
{
	public class PercentageImpactedTestsStatistic : ITestProcessingResult
	{
		public double PercentageImpactedTests { get; set; }

		public string DeltaIdentifier { get; set; }
	}
}