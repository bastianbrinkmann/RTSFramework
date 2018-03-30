using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Controller.RunConfigurations
{
    public class RunConfiguration<TPe> where TPe : CSharpProgramModel
    {

        public GranularityLevel GranularityLevel { get; set; }
        public ProcessingType ProcessingType { get; set; }

        public DiscoveryType DiscoveryType { get; set; }

        public RTSApproachType RTSApproachType { get; set; }

        public TPe OldProgramModel { get; set; }

        public TPe NewProgramModel { get; set; }

        public string GitRepositoryPath { get; set; }

        public string AbsoluteSolutionPath { get; set; }
    }
}