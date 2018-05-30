using RTSFramework.Contracts.Models;

namespace RTSFramework.ViewModels.RunConfigurations
{
	public class UserSettings
	{
		public ProgramModelType ProgramModelType { get; set; }
		public TestType TestType { get; set; }
		public DiscoveryType DiscoveryType { get; set; }
		public ProcessingType ProcessingType { get; set; }
		public RTSApproachType RTSApproachType { get; set; }
		public GranularityLevel GranularityLevel { get; set; }
		public string SolutionFilePath { get; set; }
		public string RepositoryPath { get; set; }
		public bool WithTimeLimit { get; set; }
		public double TimeLimit { get; set; }
		public string TestCaseNameFilter { get; set; }
		public string ClassNameFilter { get; set; }
		public string CategoryFilter { get; set; }
		public string CsvTestsFile { get; set; }
	}
}