using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
    public interface ITestProcessor<TTc> where TTc : ITestCase
    {
        void ProcessTests(IEnumerable<TTc> tests);
    }
}