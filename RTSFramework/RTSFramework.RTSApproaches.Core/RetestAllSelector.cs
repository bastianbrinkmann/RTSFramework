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
	public class RetestAllSelector<TModel, TModelElement, TTestCase> : ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>
		where TModel : IProgramModel
		where TModelElement : IProgramModelElement
		where TTestCase : class, ITestCase
	{

		public Task SelectTests(StructuralDelta<TestsModel<TTestCase>, TTestCase> testsDelta, StructuralDelta<TModel, TModelElement> delta, CancellationToken cancellationToken)
		{
			SelectedTests = testsDelta.NewModel.TestSuite;

			return Task.CompletedTask;
		}

		public ISet<TTestCase> SelectedTests { get; private set; }
		public ICorrespondenceModel CorrespondenceModel => null;
	}
}