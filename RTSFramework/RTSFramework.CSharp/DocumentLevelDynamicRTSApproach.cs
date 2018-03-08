using System.Collections.Generic;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Contracts;
using RTSFramework.Core;

namespace RTSFramework.Concrete.CSharp
{
    public class DocumentLevelDynamicRTSApproach : IRTSApproach<OperationalDelta<CSharpDocument>, CSharpDocument, MSTestTestcase>
    {
        public IEnumerable<MSTestTestcase> PerformRTS(IEnumerable<MSTestTestcase> testCases, OperationalDelta<CSharpDocument> delta)
        {
            throw new System.NotImplementedException();
        }
    }
}