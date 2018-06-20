using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts
{
    public interface ITestProcessor<TTestCase, TResult, TDelta, TModel> where TTestCase : ITestCase where TResult : ITestProcessingResult where TDelta : IDelta<TModel> where TModel : IProgramModel
	{
        Task<TResult> ProcessTests(IList<TTestCase> impactedTests, StructuralDelta<ISet<TTestCase>, TTestCase> testsDelta, TDelta impactedForDelta, CancellationToken cancellationToken);
    }
}