using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.TFS2010.Models
{
    public class TFS2010ProgramModel : ICSharpProgramModel
    {
        public string VersionId { get; set; }
        public string AbsoluteSolutionPath { get; set; }
    }
}