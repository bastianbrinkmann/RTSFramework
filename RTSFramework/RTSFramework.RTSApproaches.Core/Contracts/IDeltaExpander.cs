using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.RTSApproaches.Core.Contracts
{
	/// <summary>
	/// Called Delta Expansion as in:
	/// https://link.springer.com/chapter/10.1007/978-3-642-34026-0_9
	/// A Generic Platform for Model-Based Regression Testing
	/// by Zech et al.
	/// </summary>
	public interface IDeltaExpander<TModel, TTestCase, TDelta, TDataStructure>
		where TModel : IProgramModel
		where TTestCase : ITestCase 
		where TDelta : IDelta<TModel>
	{
		ISet<TTestCase> SelectedTests { get; }

		Func<string, IList<string>> GetResponsibleChangesByTestId { get; }

		Task ExpandDelta(ISet<TTestCase> testCases, TDelta delta, TDataStructure dataStructure, CancellationToken cancellationToken);
	}
}