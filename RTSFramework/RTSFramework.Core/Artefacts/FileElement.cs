using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Core.Artefacts
{
    public class FileElement : IProgramModelElement
    {
        public string Id { get; }

        public FileElement(string filePath)
        {
            Id = filePath;
        }
    }
}