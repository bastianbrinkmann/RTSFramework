using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
	public interface ITestsInstrumentor<TModel, TTestCase> : IDisposable where TModel : IProgramModel where TTestCase : ITestCase
	{
		Task Instrument(TModel toInstrument, IList<TTestCase> tests, CancellationToken token);

		CoverageData GetCoverageData();
	}
}