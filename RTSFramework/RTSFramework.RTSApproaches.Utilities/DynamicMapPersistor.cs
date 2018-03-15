using System;
using System.IO;
using Newtonsoft.Json;

namespace RTSFramework.RTSApproaches.Utilities
{
    internal static class DynamicMapPersistor
    {
        private const string MapStoragePlace = "TestCasesToProgramMaps";
        private const string FileExtension = ".json";

        internal static void PersistTestCasestoProgramMap(TestCasesToProgramMap map)
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
                    var serializer = new JsonSerializer();
                    serializer.Serialize(writer, map);
                }
            }
        }

        private static FileInfo GetFile(string programVersionId)
        {
            return new FileInfo(Path.Combine(MapStoragePlace, Uri.EscapeUriString(programVersionId) + FileExtension));
        }

        internal static TestCasesToProgramMap LoadTestCasesToProgramMap(string programVersionId)
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
                                var serializer = new JsonSerializer();
                                return serializer.Deserialize<TestCasesToProgramMap>(jsonReader);
                            }
                        }
                    }
                }
            }

            return new TestCasesToProgramMap{ProgramVersionId = programVersionId};
        }
    }
}