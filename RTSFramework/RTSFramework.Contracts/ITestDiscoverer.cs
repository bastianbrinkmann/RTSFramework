using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts
{
	public interface ITestDiscoverer<TProgram, TProgramDelta, TTestCase> where TTestCase : ITestCase where TProgram : IProgramModel where TProgramDelta: IDelta<TProgram>
	{
        Task<StructuralDelta<TestsModel<TTestCase>, TTestCase>> GetTestsDelta(TProgramDelta programDelta, Func<TTestCase, bool> filterFunction, CancellationToken token);
	}
}