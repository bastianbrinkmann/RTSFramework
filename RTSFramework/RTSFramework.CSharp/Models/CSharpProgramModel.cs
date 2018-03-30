using System.IO;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.CSharp.Core.Models
{
    public abstract class CSharpProgramModel : IProgramModel
    {
        public string AbsoluteSolutionPath { get; set;  }
        public string VersionId { get; set; }
        public string RootPath => Path.GetDirectoryName(AbsoluteSolutionPath);
    }
}