using System.Collections.Generic;
using System.Linq;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.Delta;
using RTSFramework.Core.Artefacts;

namespace RTSFramework.Concrete.User
{
    //TODO: This is a delta provider not a discoverer
    public class UserIntendedChangesDiscoverer<TP> : IOfflineDeltaDiscoverer<TP, StructuralDelta<FileElement>> where TP : IProgramModel
    {
        private readonly List<FileElement> changedFiles;

        public UserIntendedChangesDiscoverer(IEnumerable<string> filePaths)
        {
            changedFiles = filePaths.Select(x => new FileElement(x)).ToList();
        }

        public StructuralDelta<FileElement> Discover(TP oldVersion, TP newVersion)
        {
            var delta = new StructuralDelta<FileElement>
            {
                SourceModelId = oldVersion.VersionId,
                TargetModelId = newVersion.VersionId
            };

            delta.ChangedElements.AddRange(changedFiles);

            return delta;
        }
    }
}