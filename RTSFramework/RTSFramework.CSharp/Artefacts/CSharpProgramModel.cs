using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.CSharp.Core.Artefacts
{
    //TODO Remove?
    public class CSharpProgramModel : IProgramModel
    {
        public string VersionId { get; set; }
        public string SolutionPath { get; set; }
    }
}