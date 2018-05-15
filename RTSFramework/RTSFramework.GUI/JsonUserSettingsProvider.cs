using System;
using System.IO;
using Newtonsoft.Json;
using RTSFramework.Contracts.Models;
using RTSFramework.ViewModels.RunConfigurations;

namespace RTSFramework.GUI
{
	public class JsonUserSettingsProvider : IUserSettingsProvider
	{
		private const string UserSettingsFileName = "usersettings.json";

		public void LoadSettings()
		{
			var userSettingsFile = new FileInfo(Path.GetFullPath(UserSettingsFileName));

			if (userSettingsFile.Exists)
			{
				using (FileStream stream = userSettingsFile.Open(FileMode.Open, FileAccess.Read))
				{
					using (StreamReader streamReader = new StreamReader(stream))
					{
						using (JsonTextReader jsonReader = new JsonTextReader(streamReader))
						{
							var serializer = JsonSerializer.Create(new JsonSerializerSettings {Formatting = Formatting.Indented});
							var settings = serializer.Deserialize<JsonUserSettingsProvider>(jsonReader);
							ProgramModelType = settings.ProgramModelType;
							DiscoveryType = settings.DiscoveryType;
							ProcessingType = settings.ProcessingType;
							RTSApproachType = settings.RTSApproachType;
							GranularityLevel = settings.GranularityLevel;
							SolutionFilePath = settings.SolutionFilePath;
							RepositoryPath = settings.RepositoryPath;
						}
					}
				}
			}
			else
			{
				ProgramModelType = ProgramModelType.GitModel;
				DiscoveryType = DiscoveryType.GitDiscovery;
				ProcessingType = ProcessingType.MSTestExecution;
				RTSApproachType = RTSApproachType.DynamicRTS;
				GranularityLevel = GranularityLevel.Class;
				SolutionFilePath = @"";
				RepositoryPath = @"";
			}
		}

		public ProgramModelType ProgramModelType { get; set; }
		public DiscoveryType DiscoveryType { get; set; }
		public ProcessingType ProcessingType { get; set; }
		public RTSApproachType RTSApproachType { get; set; }
		public GranularityLevel GranularityLevel { get; set; }
		public string SolutionFilePath { get; set; }
		public string RepositoryPath { get; set; }

		public double TimeLimit { get; set; }

		public void Dispose()
		{
			SaveSettings();
		}

		private void SaveSettings()
		{
			var userSettingsFile = new FileInfo(Path.GetFullPath(UserSettingsFileName));

			using (FileStream stream = userSettingsFile.Open(FileMode.OpenOrCreate, FileAccess.Write))
			{
				using (StreamWriter writer = new StreamWriter(stream))
				{
					var serializer = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented });
					serializer.Serialize(writer, this);
				}
			}
		}
	}
}