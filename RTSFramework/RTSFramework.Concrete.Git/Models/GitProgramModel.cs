using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.Git.Models
{
    public class GitProgramModel : CSharpProgramModel
    {
        public GitVersionReferenceType GitVersionReferenceType { get; set; }
        public string CommitId { get; set; }
        public string RepositoryPath { get; set; }
    }
}