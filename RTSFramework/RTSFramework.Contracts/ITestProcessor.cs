using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
    public interface ITestProcessor<TTc> where TTc : ITestCase
    {
        Task ProcessTests(IEnumerable<TTc> tests, CancellationToken cancellationToken = default(CancellationToken));
    }
}