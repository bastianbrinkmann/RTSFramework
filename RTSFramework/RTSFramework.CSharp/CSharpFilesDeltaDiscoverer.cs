using System.Linq;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;

namespace RTSFramework.Concrete.CSharp.Core
{
    public class CSharpFilesDeltaDiscoverer<TP> : IOfflineDeltaDiscoverer<TP, StructuralDelta<TP, CSharpFileElement>>
        where TP : IProgramModel
    {
        private readonly IOfflineDeltaDiscoverer<TP, StructuralDelta<TP, FileElement>> internalDiscoverer;

		public DiscoveryType DiscoveryType => internalDiscoverer.DiscoveryType;

		public CSharpFilesDeltaDiscoverer(IOfflineDeltaDiscoverer<TP, StructuralDelta<TP, FileElement>> internalDiscoverer)
        {
            this.internalDiscoverer = internalDiscoverer;
        }

        public StructuralDelta<TP, CSharpFileElement> Discover(TP oldModel, TP newModel)
        {
            var fileDelta = internalDiscoverer.Discover(oldModel, newModel);
            return Convert(fileDelta);
        }

        public StructuralDelta<TP, CSharpFileElement> Convert(StructuralDelta<TP, FileElement> delta)
        {
            StructuralDelta<TP, CSharpFileElement> result = new StructuralDelta<TP, CSharpFileElement>
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