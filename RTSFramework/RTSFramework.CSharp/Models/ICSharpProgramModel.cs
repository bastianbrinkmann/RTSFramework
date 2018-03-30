using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.CSharp.Core.Models
{
    public interface ICSharpProgramModel : IProgramModel
    {
        string AbsoluteSolutionPath { get; set;  }
    }
}