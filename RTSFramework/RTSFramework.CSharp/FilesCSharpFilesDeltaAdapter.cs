using System.Linq;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;

namespace RTSFramework.Concrete.CSharp.Core
{
	public class FilesCSharpFilesDeltaAdapter<TModel> : IDeltaAdapter<StructuralDelta<TModel, FileElement>, StructuralDelta<TModel, CSharpFileElement>, TModel>
		where TModel : IProgramModel
	{
		public StructuralDelta<TModel, CSharpFileElement> Convert(StructuralDelta<TModel, FileElement> delta)
		{
			var result = new StructuralDelta<TModel, CSharpFileElement>(delta.OldModel, delta.NewModel);

			result.AddedElements.AddRange(delta.AddedElements.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id, x.GetContent)));
			result.ChangedElements.AddRange(delta.ChangedElements.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id, x.GetContent)));
			result.DeletedElements.AddRange(delta.DeletedElements.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id, x.GetContent)));

			return result;
		}
	}
}