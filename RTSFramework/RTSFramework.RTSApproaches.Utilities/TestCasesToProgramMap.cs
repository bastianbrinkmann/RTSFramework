using System.Collections.Generic;

namespace RTSFramework.RTSApproaches.Utilities
{
    public class TestCasesToProgramMap
    {
        public string ProgramVersionId { get; set; }

        public ISet<Dictionary<string, HashSet<string>>> TestCaseToProgramElementsMap { get; set; }
            = new HashSet<Dictionary<string, HashSet<string>>>();
    }
}