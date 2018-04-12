using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;
using RTSFramework.Core.Utilities;

namespace RTSFramework.Concrete.User
{
	public class UserIntendedChangesDiscoverer<TModel> : IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, FileElement>> where TModel : IProgramModel
	{
		private readonly IIntededChangesProvider intededChangesProvider;

		public UserIntendedChangesDiscoverer(IIntededChangesProvider intededChangesProvider)
		{
			this.intededChangesProvider = intededChangesProvider;
		}

		public StructuralDelta<TModel, FileElement> Discover(TModel oldVersion, TModel newVersion)
		{
			var delta = new StructuralDelta<TModel, FileElement>(oldVersion, newVersion);

			foreach (string intendedChange in intededChangesProvider.IntendedChanges)
			{
				var relativePathToSolution = RelativePathHelper.GetRelativePath(newVersion, intendedChange);
				delta.ChangedElements.Add(new FileElement(relativePathToSolution));
			}

			return delta;
		}
	}
}