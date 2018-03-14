using System;
using System.IO;
using Newtonsoft.Json;

namespace RTSFramework.RTSApproaches.Utilities
{
    public static class DynamicMapPersistor
    {
        private const string MapStoragePlace = "TestCasesToProgramMaps";

        public static void PersistTestCasestoProgramMap(TestCasesToProgramMap map)
        {
            if (!Directory.Exists(MapStoragePlace))
            {
                Directory.CreateDirectory(MapStoragePlace);
            }

            FileInfo file = new FileInfo(Path.Combine(MapStoragePlace, Uri.EscapeUriString(map.ProgramVersionId)));

            using (FileStream stream = file.Open(FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    var serializer = new JsonSerializer();
                    serializer.Serialize(writer, map);
                }
            }
        }

        public static TestCasesToProgramMap LoadTestCasesToProgramMap(string programVersionId)
        {
            if (Directory.Exists(MapStoragePlace))
            {
                FileInfo file = new FileInfo(Path.Combine(MapStoragePlace, Uri.EscapeUriString(programVersionId)));

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

            return new TestCasesToProgramMap();
        }
    }
}