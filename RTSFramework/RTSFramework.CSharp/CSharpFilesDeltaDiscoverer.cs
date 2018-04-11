using System.Linq;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;

namespace RTSFramework.Concrete.CSharp.Core
{
    public class CSharpFilesDeltaDiscoverer: IOfflineDeltaDiscoverer
	{
        private readonly IOfflineFileDeltaDiscoverer internalDiscoverer;

		public CSharpFilesDeltaDiscoverer(IOfflineFileDeltaDiscoverer internalDiscoverer)
        {
            this.internalDiscoverer = internalDiscoverer;
        }

        public StructuralDelta Discover(IProgramModel oldModel, IProgramModel newModel)
        {
            var fileDelta = internalDiscoverer.Discover(oldModel, newModel);
            return Convert(fileDelta);
        }

        public StructuralDelta Convert(StructuralDelta delta)
        {
            StructuralDelta result = new StructuralDelta
            {
                SourceModel = delta.SourceModel,
                TargetModel = delta.TargetModel,
            };

            result.AddedElements.AddRange(delta.AddedElements.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id)));
            result.ChangedElements.AddRange(delta.ChangedElements.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id)));
            result.DeletedElements.AddRange(delta.DeletedElements.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id)));

            return result;
        }
    }
}