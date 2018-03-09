using System.Collections.Generic;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.Git.Artefacts;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.RTSApproaches.Utilities;

namespace RTSFramework.Concrete.RTS
{
    public class DocumentLevelDynamicRTSApproach : IRTSApproach<IDelta<CSharpDocument, GitProgramVersion>, CSharpDocument, GitProgramVersion, MSTestTestcase>
    {
        public IEnumerable<MSTestTestcase> PerformRTS(IEnumerable<MSTestTestcase> testCases, IDelta<CSharpDocument, GitProgramVersion> delta)
        {
            var map = DynamicMapPersistor.LoadTestCasesToProgramMap(delta.Source.VersionId);

            



            return testCases;
        }
    }
}