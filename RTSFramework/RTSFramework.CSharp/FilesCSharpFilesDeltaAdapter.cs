using System.Linq;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;

namespace RTSFramework.Concrete.CSharp.Core
{
	public class FilesCSharpFilesDeltaAdapter : IDeltaAdapter<StructuralDelta<FilesProgramModel, FileElement>, StructuralDelta<CSharpFilesProgramModel, CSharpFileElement>, FilesProgramModel, CSharpFilesProgramModel>
	{
		public StructuralDelta<CSharpFilesProgramModel, CSharpFileElement> Convert(StructuralDelta<FilesProgramModel, FileElement> delta)
		{
			var oldCSharpFilesModel = new CSharpFilesProgramModel
			{
				AbsoluteSolutionPath = delta.OldModel.AbsoluteSolutionPath,
				VersionId = delta.OldModel.VersionId
			};
			oldCSharpFilesModel.Files.AddRange(delta.OldModel.Files.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id, x.GetContent)));

			var newCSharpFilesModel = new CSharpFilesProgramModel
			{
				AbsoluteSolutionPath = delta.NewModel.AbsoluteSolutionPath,
				VersionId = delta.NewModel.VersionId
			};
			newCSharpFilesModel.Files.AddRange(delta.NewModel.Files.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id, x.GetContent)));


			var result = new StructuralDelta<CSharpFilesProgramModel, CSharpFileElement>(oldCSharpFilesModel, newCSharpFilesModel);

			result.AddedElements.AddRange(delta.AddedElements.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id, x.GetContent)));
			result.ChangedElements.AddRange(delta.ChangedElements.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id, x.GetContent)));
			result.DeletedElements.AddRange(delta.DeletedElements.Where(x => x.Id.EndsWith(".cs")).Select(x => new CSharpFileElement(x.Id, x.GetContent)));

			return result;
		}
	}
}