using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
    public interface ITestProcessor<TTestCase, TResult> where TTestCase : ITestCase where TResult : ITestProcessingResult
	{
        Task<TResult> ProcessTests(IEnumerable<TTestCase> tests, CancellationToken cancellationToken);
    }
}