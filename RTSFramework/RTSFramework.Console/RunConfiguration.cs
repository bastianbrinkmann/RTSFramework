using RTSFramework.Console.RunConfigurations;

namespace RTSFramework.Console
{
    public class RunConfiguration
    {
        public ProcessingType ProcessingType { get; set; }
        public DiscoveryType DiscoveryType { get; set; }

        public ProgramModelType ProgramModelType { get; set; }

        public RTSApproachType RTSApproachType { get; set; }

        public string GitRepositoryPath { get; set; }

        public string[] IntendedChanges { get; set; }

        public string[] TestAssemblyFolders { get; set; }

        public bool PersistDynamicMap { get; set; }
    }
}