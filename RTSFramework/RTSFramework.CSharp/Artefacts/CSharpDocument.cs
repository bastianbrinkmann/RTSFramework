using Microsoft.CodeAnalysis;

namespace RTSFramework.Concrete.CSharp.Artefacts
{
    public class CSharpDocument : ICSharpProgramElement
    {
        public string Id { get; }

        public CSharpDocument(string filePath)
        {
            Id = filePath;
        }
    }
}