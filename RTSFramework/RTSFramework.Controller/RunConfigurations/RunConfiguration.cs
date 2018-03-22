using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Controller.RunConfigurations
{
    public class RunConfiguration<TPe> where TPe : IProgramModel
    {
        public ProcessingType ProcessingType { get; set; }

        public DiscoveryType DiscoveryType { get; set; }

        public RTSApproachType RTSApproachType { get; set; }

        public string[] TestAssemblyFolders { get; set; }

        public TPe OldProgramModel { get; set; }

        public TPe NewProgramModel { get; set; }
        public string GitRepositoryPath { get; set; }
    }
}