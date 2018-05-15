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
		where TTestCase : ITestCase
	{
		private StructuralDelta<TModel, TModelElement> currentDelta;

		public Task<IList<TTestCase>> SelectTests(IList<TTestCase> testCases, StructuralDelta<TModel, TModelElement> delta, CancellationToken cancellationToken)
		{
			currentDelta = delta;
			return Task.FromResult(testCases);
		}

		public IResponsibleChangesProvider GetResponsibleChangesProvider()
		{
			return new RetestAllResponsibleChangesProvider(currentDelta);
		}

		private class RetestAllResponsibleChangesProvider : IResponsibleChangesProvider
		{
			private readonly StructuralDelta<TModel, TModelElement> delta;

			public RetestAllResponsibleChangesProvider(StructuralDelta<TModel, TModelElement> delta)
			{
				this.delta = delta;
			}

			public IList<string> GetResponsibleChangesForImpactedTest(string testCaseId)
			{
				return delta.AddedElements.Select(x => x.Id)
				.Union(delta.ChangedElements.Select(x => x.Id))
				.Union(delta.DeletedElements.Select(x => x.Id)).ToList();
			}
		}
	}
}