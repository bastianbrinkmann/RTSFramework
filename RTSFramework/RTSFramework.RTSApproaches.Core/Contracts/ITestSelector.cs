using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.RTSApproaches.Core.Contracts
{
	public interface ITestSelector<TModel, TProgramDelta, TTestCase>
		where TModel : IProgramModel
		where TProgramDelta : IDelta<TModel>
		where TTestCase : ITestCase
	{
		ISet<TTestCase> SelectedTests { get; }

		ICorrespondenceModel CorrespondenceModel { get; }

		Task SelectTests(StructuralDelta<ISet<TTestCase>, TTestCase> testsDelta, TProgramDelta delta, CancellationToken cancellationToken);
	}
}