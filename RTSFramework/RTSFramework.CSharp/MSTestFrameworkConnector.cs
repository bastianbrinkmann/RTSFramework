using System.Collections.Generic;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.CSharp
{
    public class MSTestFrameworkConnector : IAutomatedTestFramework<MSTestTestcase>
    {
        public IEnumerable<MSTestTestcase> GetTestCases()
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ITestCaseResult<MSTestTestcase>> ExecuteTests(IEnumerable<MSTestTestcase> tests)
        {
            throw new System.NotImplementedException();
        }
    }
}