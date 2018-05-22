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
	public class RetestAllSelector<TModel, TModelElement, TTestCase> : ITestsSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>
		where TModel : IProgramModel
		where TModelElement : IProgramModelElement
		where TTestCase : class, ITestCase
	{

		public Task<IList<TTestCase>> SelectTests(IList<TTestCase> testCases, StructuralDelta<TModel, TModelElement> delta, CancellationToken cancellationToken)
		{
			var allChangedElements = delta.AddedElements.Select(x => x.Id)
				.Union(delta.ChangedElements.Select(x => x.Id))
				.Union(delta.DeletedElements.Select(x => x.Id)).ToList();

			foreach (var testCase in testCases)
			{
				testCase.GetResponsibleChangesForLastImpact = () => allChangedElements;
			}

			return Task.FromResult(testCases);
		}
	}
}