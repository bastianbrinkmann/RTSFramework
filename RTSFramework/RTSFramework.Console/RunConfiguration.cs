using RTSFramework.Console.RunConfigurations;

namespace RTSFramework.Console
{
    public class RunConfiguration
    {
        public DiscoveryType DiscoveryType { get; set; }

        public ProgramModelType ProgramModelType { get; set; }

        public RTSApproachType RTSApproachType { get; set; }

        public string GitRepositoryPath { get; set; }

        public string[] IntendedChanges { get; set; }

        public string[] TestAssemblies { get; set; }

        public bool PersistDynamicMap { get; set; }
    }
}