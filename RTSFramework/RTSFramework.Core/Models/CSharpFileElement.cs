using System;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Core.Models
{
    public class CSharpFileElement : IProgramModelElement
    {
        public string Id { get; }

		public Func<string> GetContent { get; }

		public CSharpFileElement(string filePath, Func<string> getContent)
        {
            Id = filePath;
	        GetContent = getContent;
        }
    }
}