using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
	public interface ITestDiscoverer<TModel, TTestCase> where TTestCase : ITestCase where TModel : IProgramModel
	{
        Task<ISet<TTestCase>> GetTests(TModel model, Func<TTestCase, bool> filterFunction, CancellationToken token);
	}
}