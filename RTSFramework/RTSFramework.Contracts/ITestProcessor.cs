using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts
{
    public interface ITestProcessor<TTestCase, TResult, TProgramDelta, TModel> where TTestCase : ITestCase where TResult : ITestProcessingResult where TProgramDelta : IDelta<TModel> where TModel : IProgramModel
	{
        Task<TResult> ProcessTests(IList<TTestCase> impactedTests, StructuralDelta<TestsModel<TTestCase>, TTestCase> testsDelta, TProgramDelta impactedForDelta, CancellationToken cancellationToken);
    }
}