using RTSFramework.Contracts.Models;

namespace RTSFramework.Core.Models
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