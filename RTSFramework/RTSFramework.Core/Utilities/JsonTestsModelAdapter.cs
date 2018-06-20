using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Core.Utilities
{
	public class JsonTestsModelAdapter<TTestCase> : IArtefactAdapter<FileInfo, ISet<TTestCase>> where TTestCase : ITestCase
	{
		public const string FileExtension = ".json";

		public ISet<TTestCase> Parse(FileInfo artefact)
		{
			if (artefact.Extension != FileExtension)
			{
				throw new ArgumentException("Json Tests Model Adapter can only convert json files!", nameof(artefact));
			}

			if (artefact.Exists)
			{
				using (FileStream stream = artefact.Open(FileMode.Open, FileAccess.Read))
				{
					using (StreamReader streamReader = new StreamReader(stream))
					{
						using (JsonTextReader jsonReader = new JsonTextReader(streamReader))
						{
							var serializer = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented });
							return serializer.Deserialize<ISet<TTestCase>>(jsonReader);
						}
					}
				}
			}

			return null;
		}

		public FileInfo Unparse(ISet<TTestCase> model, FileInfo artefact)
		{
			if (artefact.DirectoryName != null && !Directory.Exists(artefact.DirectoryName))
			{
				Directory.CreateDirectory(artefact.DirectoryName);
			}

			using (FileStream stream = artefact.Open(FileMode.OpenOrCreate, FileAccess.Write))
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