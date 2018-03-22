﻿using System;
using System.IO;
using Newtonsoft.Json;

namespace RTSFramework.RTSApproaches.Utilities
{
    internal static class DynamicMapPersistor
    {
        private const string MapStoragePlace = "TestCasesToProgramMaps";
        private const string FileExtension = ".json";

        internal static void PersistTestCasestoProgramMap(TransitiveClosureTestDependencies map)
        {
            if (!Directory.Exists(MapStoragePlace))
            {
                Directory.CreateDirectory(MapStoragePlace);
            }

            FileInfo file = GetFile(map.ProgramVersionId);

            using (FileStream stream = file.Open(FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    var serializer = JsonSerializer.Create(new JsonSerializerSettings {Formatting = Formatting.Indented});
                    serializer.Serialize(writer, map);
                }
            }
        }

        private static FileInfo GetFile(string programVersionId)
        {
            return new FileInfo(Path.Combine(MapStoragePlace, Uri.EscapeUriString(programVersionId) + FileExtension));
        }

        internal static TransitiveClosureTestDependencies LoadTestCasesToProgramMap(string programVersionId)
        {
            if (Directory.Exists(MapStoragePlace))
            {
                FileInfo file = GetFile(programVersionId);

                if (file.Exists)
                {
                    using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(stream))
                        {
                            using (JsonTextReader jsonReader = new JsonTextReader(streamReader))
                            {
								var serializer = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented });
								return serializer.Deserialize<TransitiveClosureTestDependencies>(jsonReader);
                            }
                        }
                    }
                }
            }

            return new TransitiveClosureTestDependencies{ProgramVersionId = programVersionId};
        }
    }
}