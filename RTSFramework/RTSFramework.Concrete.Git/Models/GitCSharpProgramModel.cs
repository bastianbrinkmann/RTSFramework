using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.Git.Models
{
    public class GitCSharpProgramModel : CSharpProgramModel
    {
		public GitVersionIdentification GitVersionIdentification { get; set; }
    }
}