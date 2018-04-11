using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;
using RTSFramework.Core.Utilities;

namespace RTSFramework.Concrete.User
{
    public class UserIntendedChangesDiscoverer : IOfflineFileDeltaDiscoverer
	{
		public StructuralDelta Discover(IProgramModel oldVersion, IProgramModel newVersion)
        {
            var delta = new StructuralDelta
            {
                SourceModel = oldVersion,
                TargetModel = newVersion
            };

            //TODO Should be Independent of console, so add additional interface
            //Console.WriteLine("Intended Changes (absolute files - \"Done\" once list is complete):");
            //string line = Console.ReadLine();

            //while (line != null && !line.Equals("Done"))
            //{
            //    delta.ChangedElements.Add(new FileElement(line));
            //    line = Console.ReadLine();
            //} 
            var relativePathToSolution = RelativePathHelper.GetRelativePath(newVersion, @"C:\Git\TIATestProject\MainProject\Calculator.cs");

            delta.ChangedElements.Add(new FileElement(relativePathToSolution));

            

            return delta;
        }
    }
}