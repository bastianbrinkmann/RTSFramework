using System;
using System.Collections.Generic;
using System.Linq;
using RTSFramework.Contracts;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;

namespace RTSFramework.Concrete.User
{
    //TODO: This is a delta provider not a discoverer
    public class UserIntendedChangesDiscoverer<TP> : IOfflineDeltaDiscoverer<TP, StructuralDelta<FileElement>> where TP : IProgramModel
    {
        public StructuralDelta<FileElement> Discover(TP oldVersion, TP newVersion)
        {
            var delta = new StructuralDelta<FileElement>
            {
                SourceModelId = oldVersion.VersionId,
                TargetModelId = newVersion.VersionId
            };

            //TODO Should be Independent of console, so add additional interface
            //Console.WriteLine("Intended Changes (absolute files - \"Done\" once list is complete):");
            //string line = Console.ReadLine();

            //while (line != null && !line.Equals("Done"))
            //{
            //    delta.ChangedElements.Add(new FileElement(line));
            //    line = Console.ReadLine();
            //} 

            delta.ChangedElements.Add(new FileElement(@"C:\Git\TIATestProject\MainProject\Calculator.cs"));

            return delta;
        }
    }
}