using System.IO;
using RTSFramework.Concrete.User.Models;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;
using RTSFramework.Core.Utilities;

namespace RTSFramework.Concrete.User
{
	public class IntendedChangesAdapter : IArtefactAdapter<IntendedChangesArtefact, StructuralDelta<FilesProgramModel, FileElement>>
	{
		public StructuralDelta<FilesProgramModel, FileElement> Parse(IntendedChangesArtefact artefact)
		{
			var delta = new StructuralDelta<FilesProgramModel, FileElement>(artefact.ProgramModel, artefact.ProgramModel);

			foreach (string intendedChange in artefact.IntendedChanges)
			{
				var relativePathToSolution = RelativePathHelper.GetRelativePath(artefact.ProgramModel, intendedChange);
				delta.ChangedElements.Add(new FileElement(relativePathToSolution, () => File.ReadAllText(intendedChange)));
			}

			return delta;
		}

		public IntendedChangesArtefact Unparse(StructuralDelta<FilesProgramModel, FileElement> model, IntendedChangesArtefact artefact)
		{
			throw new System.NotImplementedException();
		}
	}
}