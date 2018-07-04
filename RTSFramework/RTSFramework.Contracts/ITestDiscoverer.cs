using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts
{
	public interface ITestDiscoverer<TModel, TDelta, TTestCase> where TTestCase : ITestCase where TModel : IProgramModel where TDelta: IDelta<TModel>
	{
        Task<StructuralDelta<TestsModel<TTestCase>, TTestCase>> GetTests(TDelta delta, Func<TTestCase, bool> filterFunction, CancellationToken token);
	}
}