using System;
using RTSFramework.Contracts.Models;

namespace RTSFramework.ViewModels.RunConfigurations
{
	public interface IUserSettingsProvider : IDisposable
	{
		void LoadSettings();

		ProgramModelType ProgramModelType { get; set; }
		DiscoveryType DiscoveryType { get; set; }
		ProcessingType ProcessingType { get; set; }
		RTSApproachType RTSApproachType { get; set; }
		GranularityLevel GranularityLevel { get; set; }
		string SolutionFilePath { get; set; }
		string RepositoryPath { get; set; }
		double TimeLimit { get; set; }
	}
}