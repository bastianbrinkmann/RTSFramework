using System.IO;
using Newtonsoft.Json;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.ViewModels.RunConfigurations;

namespace RTSFramework.ViewModels.Adapter
{
	public class JsonFileUserSettingsAdapter : IArtefactAdapter<string, UserSettings>
	{
		public UserSettings Parse(string artefact)
		{
			UserSettings settings;

			var userSettingsFile = new FileInfo(Path.GetFullPath(artefact));

			using (FileStream stream = userSettingsFile.Open(FileMode.Open, FileAccess.Read))
			{
				using (StreamReader streamReader = new StreamReader(stream))
				{
					using (JsonTextReader jsonReader = new JsonTextReader(streamReader))
					{
						var serializer = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented });
						settings = serializer.Deserialize<UserSettings>(jsonReader);
					}
				}
			}

			return settings;
		}

		public string Unparse(UserSettings model, string artefact)
		{
			var userSettingsFile = new FileInfo(Path.GetFullPath(artefact));

			using (FileStream stream = userSettingsFile.Open(FileMode.OpenOrCreate, FileAccess.Write))
			{
				using (StreamWriter writer = new StreamWriter(stream))
				{
					var serializer = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented });
					serializer.Serialize(writer, model);
				}
			}

			return artefact;
		}
	}
}