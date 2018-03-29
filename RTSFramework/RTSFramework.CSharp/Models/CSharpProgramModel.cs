using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.CSharp.Core.Models
{
    //TODO Remove?
    public class CSharpProgramModel : IProgramModel
    {
        public string VersionId { get; set; }
        public string SolutionPath { get; set; }
    }
}