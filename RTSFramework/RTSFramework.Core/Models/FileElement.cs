using RTSFramework.Contracts.Models;

namespace RTSFramework.Core.Models
{
	//TODO: Not only Id but also store file content?
    public class FileElement : IProgramModelElement
    {
        public string Id { get; }

        public FileElement(string filePath)
        {
            Id = filePath;
        }
    }
}