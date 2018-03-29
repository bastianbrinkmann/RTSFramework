using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.CSharp.Roslyn.Models
{
    public class CSharpClassElement : IProgramModelElement
    { 
        public string Id { get; }

        public CSharpClassElement(string fullClassName)
        {
            Id = fullClassName;
        }
    }
}