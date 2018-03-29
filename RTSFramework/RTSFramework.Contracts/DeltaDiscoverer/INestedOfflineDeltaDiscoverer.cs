using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.DeltaDiscoverer
{
	public interface INestedOfflineDeltaDiscoverer<TP,TD,TDIntermediate> : IOfflineDeltaDiscoverer<TP, TD> 
        where TP : IProgramModel
        where TD: IDelta
        where TDIntermediate: IDelta

    {
        IOfflineDeltaDiscoverer<TP, TDIntermediate> IntermediateDeltaDiscoverer { get; set; }
    }
}