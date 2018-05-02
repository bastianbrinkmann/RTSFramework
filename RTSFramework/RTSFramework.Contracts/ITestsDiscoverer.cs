using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
	public interface ITestsDiscoverer<TModel, TTestCase> where TTestCase : ITestCase where TModel : IProgramModel
	{
        Task<IList<TTestCase>> GetTestCasesForModel(TModel model, CancellationToken token);
	}
}