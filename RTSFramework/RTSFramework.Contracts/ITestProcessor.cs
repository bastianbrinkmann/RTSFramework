using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
    public interface ITestProcessor<TTc> where TTc : ITestCase
    {
        void ProcessTests(IEnumerable<TTc> tests);
    }
}