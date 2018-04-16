using System.Linq;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;

namespace RTSFramework.Concrete.CSharp.Core
{
    public class CSharpFilesDeltaDiscoverer<TModel>: IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, CSharpFileElement>>  where TModel : IProgramModel
	{
        private readonly IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, FileElement>> internalDiscoverer;

		public CSharpFilesDeltaDiscoverer(IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, FileElement>> internalDiscoverer)
        {
            this.internalDiscoverer = internalDiscoverer;
        }

        public StructuralDelta<TModel, CSharpFileElement> Discover(TModel oldModel, TModel newModel)
        {
            var fileDelta = internalDiscoverer.Discover(oldModel, newModel);
            return Convert(fileDelta);
        }

        public StructuralDelta<TModel, CSharpFileElement> Convert(StructuralDelta<TModel, FileElement> delta)
        {
	        var result = new StructuralDelta<TModel, CSharpFileElement>(delta.SourceModel, delta.TargetModel);

            result.AddedElements.AddRange(delta.AddedElements.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id, x.GetContent)));
            result.ChangedElements.AddRange(delta.ChangedElements.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id, x.GetContent)));
            result.DeletedElements.AddRange(delta.DeletedElements.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id, x.GetContent)));

            return result;
        }
    }
}