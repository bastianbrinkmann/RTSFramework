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

		public Task SelectTests(ISet<TTestCase> testCases, StructuralDelta<TModel, TModelElement> delta, CancellationToken cancellationToken)
		{
			var allChangedElements = delta.AddedElements.Select(x => x.Id)
				.Union(delta.ChangedElements.Select(x => x.Id))
				.Union(delta.DeletedElements.Select(x => x.Id)).ToList();

			GetResponsibleChangesByTestId = x => allChangedElements;

			SelectedTests = testCases;

			return Task.CompletedTask;
		}

		public ISet<TTestCase> SelectedTests { get; private set; }
		public Func<string, IList<string>> GetResponsibleChangesByTestId { get; private set; }
	}
}