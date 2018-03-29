using System.Linq;
using RTSFramework.Concrete.CSharp.Core.Artefacts;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;

namespace RTSFramework.Concrete.CSharp.Core
{
    public class CSharpFilesDeltaDiscoverer<TP> : INestedOfflineDeltaDiscoverer<TP, StructuralDelta<CSharpFileElement>, StructuralDelta<FileElement>>
        where TP : IProgramModel
    {
        public IOfflineDeltaDiscoverer<TP, StructuralDelta<FileElement>> IntermediateDeltaDiscoverer { get; set; }

        public StructuralDelta<CSharpFileElement> Discover(TP oldModel, TP newModel)
        {
            var fileDelta = IntermediateDeltaDiscoverer.Discover(oldModel, newModel);
            return Convert(fileDelta);
        }

        public StructuralDelta<CSharpFileElement> Convert(StructuralDelta<FileElement> delta)
        {
            StructuralDelta<CSharpFileElement> result = new StructuralDelta<CSharpFileElement>
            {
                SourceModelId = delta.SourceModelId,
                TargetModelId = delta.TargetModelId,
            };

            result.AddedElements.AddRange(delta.AddedElements.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id)));
            result.ChangedElements.AddRange(delta.ChangedElements.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id)));
            result.DeletedElements.AddRange(delta.DeletedElements.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id)));

            return result;
        }
    }
}