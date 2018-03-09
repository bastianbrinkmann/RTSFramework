using System.Collections.Generic;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core;

namespace RTSFramework.Concrete.CSharp
{
    public class DocumentLevelDynamicRTSApproach : IRTSApproach<IDelta<CSharpDocument>, CSharpDocument, MSTestTestcase>
    {
        public IEnumerable<MSTestTestcase> PerformRTS(IEnumerable<MSTestTestcase> testCases, IDelta<CSharpDocument> delta)
        {
            return testCases;
        }
    }
}