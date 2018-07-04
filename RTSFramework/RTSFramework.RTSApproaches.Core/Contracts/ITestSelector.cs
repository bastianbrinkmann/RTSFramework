using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.RTSApproaches.Core.Contracts
{
	public interface ITestSelector<TProgram, TProgramDelta, TTestCase>
		where TProgram : IProgramModel
		where TProgramDelta : IDelta<TProgram>
		where TTestCase : ITestCase
	{
		ISet<TTestCase> SelectedTests { get; }

		ICorrespondenceModel CorrespondenceModel { get; }

		Task SelectTests(StructuralDelta<TestsModel<TTestCase>, TTestCase> testsDelta, TProgramDelta programDelta, CancellationToken cancellationToken);
	}
}