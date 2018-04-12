using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.Git.Models
{
    public class GitProgramModel : CSharpProgramModel
    {
		public GitVersionIdentification GitVersionIdentification { get; set; }
    }
}