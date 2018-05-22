using System.IO;
using RTSFramework.Concrete.User.Models;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;
using RTSFramework.Core.Utilities;

namespace RTSFramework.Concrete.User
{
	public class IntendedChangesAdapter : IArtefactAdapter<IntendedChangesArtefact, StructuralDelta<LocalProgramModel, FileElement>>
	{
		public StructuralDelta<LocalProgramModel, FileElement> Parse(IntendedChangesArtefact artefact)
		{
			var delta = new StructuralDelta<LocalProgramModel, FileElement>(artefact.LocalProgramModel, artefact.LocalProgramModel);

			foreach (string intendedChange in artefact.IntendedChanges)
			{
				var relativePathToSolution = RelativePathHelper.GetRelativePath(artefact.LocalProgramModel, intendedChange);
				delta.ChangedElements.Add(new FileElement(relativePathToSolution, () => File.ReadAllText(intendedChange)));
			}

			return delta;
		}

		public IntendedChangesArtefact Unparse(StructuralDelta<LocalProgramModel, FileElement> model, IntendedChangesArtefact artefact)
		{
			throw new System.NotImplementedException();
		}
	}
}