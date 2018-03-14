using System.Collections.Generic;

namespace RTSFramework.RTSApproaches.Utilities
{
    public class TestCasesToProgramMap
    {
        public string ProgramVersionId { get; set; }

        public Dictionary<string, HashSet<string>> TestCaseToProgramElementsMap { get; set; }
            = new Dictionary<string, HashSet<string>>();
    }
}