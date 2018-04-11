using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;

namespace RTSFramework.ViewModels.RunConfigurations
{
    public class RunConfiguration<TModel> where TModel : IProgramModel
    {

        public GranularityLevel GranularityLevel { get; set; }
        public ProcessingType ProcessingType { get; set; }

        public DiscoveryType DiscoveryType { get; set; }

        public RTSApproachType RTSApproachType { get; set; }

        public TModel OldProgramModel { get; set; }

        public TModel NewProgramModel { get; set; }

        public string GitRepositoryPath { get; set; }

        public string AbsoluteSolutionPath { get; set; }
    }
}