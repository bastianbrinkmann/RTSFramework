using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
    public interface ITestProcessor<TTestCase> where TTestCase : ITestCase
    {
        Task ProcessTests(IEnumerable<TTestCase> tests, CancellationToken cancellationToken = default(CancellationToken));
    }
}