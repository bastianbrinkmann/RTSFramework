using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.RTSApproaches.Core
{
	public class RetestAllSelector<TProgram, TProgramDelta, TTestCase> : ITestSelector<TProgram, TProgramDelta, TTestCase>
		where TProgram : IProgramModel
		where TProgramDelta : IDelta<TProgram>
		where TTestCase : class, ITestCase
	{

		public Task SelectTests(StructuralDelta<TestsModel<TTestCase>, TTestCase> testsDelta, TProgramDelta programDelta, CancellationToken cancellationToken)
		{
			SelectedTests = testsDelta.NewModel.TestSuite;

			return Task.CompletedTask;
		}

		public ISet<TTestCase> SelectedTests { get; private set; }
		public ICorrespondenceModel CorrespondenceModel => null;
	}
}