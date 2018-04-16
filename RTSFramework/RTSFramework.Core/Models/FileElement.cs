using System;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Core.Models
{
    public class FileElement : IProgramModelElement
    {
        public string Id { get; }

		public Func<string> GetContent { get; }

        public FileElement(string filePath, Func<string> getContent)
        {
            Id = filePath;
	        GetContent = getContent;
        }
    }
}