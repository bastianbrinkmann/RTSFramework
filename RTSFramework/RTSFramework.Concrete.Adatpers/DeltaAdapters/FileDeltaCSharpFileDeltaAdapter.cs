using System.Linq;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Contracts.Delta;
using RTSFramework.Core.Artefacts;

namespace RTSFramework.Concrete.Adatpers.DeltaAdapters
{
    public class FileDeltaCSharpFileDeltaAdapter : IDeltaAdapter<FileElement, CSharpFileElement>
    {
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