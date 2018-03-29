using System;
using System.IO;
using Newtonsoft.Json;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.RTSApproaches.CorrespondenceModel
{
    public class JsonCorrespondenceModelAdapter : IArtefactAdapter<FileInfo, CorrespondenceModel>
    {
        public const string FileExtension = ".json";

        public CorrespondenceModel Parse(FileInfo artefact)
        {
            if (artefact.Extension != FileExtension)
            {
                throw new ArgumentException("Json Correspondence Model Adapter can only convert json files!", nameof(artefact));
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
                            return serializer.Deserialize<CorrespondenceModel>(jsonReader);
                        }
                    }
                }
            }

            return new CorrespondenceModel { ProgramVersionId = Path.GetFileNameWithoutExtension(artefact.FullName) };
        }

        public void Unparse(CorrespondenceModel model, FileInfo artefact)
        {
            using (FileStream stream = artefact.Open(FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    var serializer = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented });
                    serializer.Serialize(writer, model);
                }
            }
        }
    }
}