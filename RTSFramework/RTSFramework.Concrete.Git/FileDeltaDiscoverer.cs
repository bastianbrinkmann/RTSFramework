using System;
using System.Linq;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;

namespace RTSFramework.Concrete.Git
{
    public class FileDeltaDiscoverer : IOfflineDeltaDiscoverer<FilesProgramModel, StructuralDelta<FilesProgramModel, FileElement>>
    {
		public StructuralDelta<FilesProgramModel, FileElement> Discover(FilesProgramModel oldVersion, FilesProgramModel newVersion)
		{
			var delta = new StructuralDelta<FilesProgramModel, FileElement>(oldVersion, newVersion);

			delta.AddedElements.AddRange(newVersion.Files.Where(x => oldVersion.Files.All(y => !x.Id.Equals(y.Id, StringComparison.Ordinal))));
			delta.DeletedElements.AddRange(oldVersion.Files.Where(x => newVersion.Files.All(y => !x.Id.Equals(y.Id, StringComparison.Ordinal))));

			var elementsExistingInBothVersion = oldVersion.Files.Where(x => newVersion.Files.Any(y => x.Id.Equals(y.Id, StringComparison.Ordinal)));

			foreach (var element in elementsExistingInBothVersion)
			{
				var oldContent = element.GetContent();
				var newContent = newVersion.Files.Single(x => x.Id == element.Id).GetContent();

				if (!oldContent.Equals(newContent, StringComparison.Ordinal))
				{
					delta.ChangedElements.Add(element);
				}
			}

			return delta;
		}
    }
}