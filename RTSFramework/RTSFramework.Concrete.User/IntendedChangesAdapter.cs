using System.IO;
using RTSFramework.Concrete.User.Models;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;
using RTSFramework.Core.Utilities;

namespace RTSFramework.Concrete.User
{
	public class IntendedChangesAdapter<TDelta> : IArtefactAdapter<IntendedChangesArtefact, TDelta> 
		where TDelta : IDelta<LocalProgramModel>
	{
		private readonly IDeltaAdapter<StructuralDelta<LocalProgramModel, FileElement>, TDelta, LocalProgramModel> deltaAdapter;

		public IntendedChangesAdapter(IDeltaAdapter<StructuralDelta<LocalProgramModel, FileElement>, TDelta, LocalProgramModel> deltaAdapter)
		{
			this.deltaAdapter = deltaAdapter;
		}

		public TDelta Parse(IntendedChangesArtefact artefact)
		{
			var delta = new StructuralDelta<LocalProgramModel, FileElement>(artefact.LocalProgramModel, artefact.LocalProgramModel);

			foreach (string intendedChange in artefact.IntendedChanges)
			{
				var relativePathToSolution = RelativePathHelper.GetRelativePath(artefact.LocalProgramModel, intendedChange);
				delta.ChangedElements.Add(new FileElement(relativePathToSolution, () => File.ReadAllText(intendedChange)));
			}

			return deltaAdapter.Convert(delta);
		}

		public void Unparse(TDelta model, IntendedChangesArtefact artefact)
		{
			throw new System.NotImplementedException();
		}
	}
}