﻿using System;
using System.IO;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;

namespace RTSFramework.ViewModels.RunConfigurations
{
	public class UserSettingsProvider : IDisposable
	{
		private readonly IArtefactAdapter<string, UserSettings> usersettingsAdapter;
		private const string UserSettingsFile = "UserRunSettings.json";

		private UserSettings settings;

		public UserSettingsProvider(IArtefactAdapter<string, UserSettings> usersettingsAdapter)
		{
			this.usersettingsAdapter = usersettingsAdapter;
		}

		public UserSettings GetUserSettings()
		{
			if (settings == null)
			{
				if (File.Exists(UserSettingsFile))
				{
					settings = usersettingsAdapter.Parse(UserSettingsFile);
				}
				else
				{
					settings = new UserSettings
					{
						ProgramLocation = ProgramLocation.GitRepository,
						TestType = TestType.MSTest,
						DiscoveryType = DiscoveryType.OfflineDiscovery,
						ProcessingType = ProcessingType.MSTestExecution,
						RTSApproachType = RTSApproachType.DynamicRTS,
						SolutionFilePath = @"",
						RepositoryPath = @"",
						TestCaseNameFilter = "",
						ClassNameFilter = "",
						CategoryFilter = "",
						TimeLimit = 60,
						CsvTestsFile = ""
					};
				}
			}

			return settings;
		}

		public void Dispose()
		{
			if (settings != null)
			{
				usersettingsAdapter.Unparse(settings, UserSettingsFile);
			}
		}
	}
}