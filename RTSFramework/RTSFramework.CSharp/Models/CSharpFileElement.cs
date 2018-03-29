using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.CSharp.Core.Artefacts
{
    public class CSharpFileElement : IProgramModelElement
    {
        public string Id { get; }

        public CSharpFileElement(string filePath)
        {
            Id = filePath;
        }
    }
}