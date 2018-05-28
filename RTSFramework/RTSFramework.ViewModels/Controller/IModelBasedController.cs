using System;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.RTSApproaches.Core;

namespace RTSFramework.ViewModels.Controller
{
	public interface IModelBasedController<TModel, TDelta, TTestCase, TResult> where TTestCase : ITestCase where TModel : IProgramModel where TDelta : IDelta<TModel>
	{
		event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
		event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
		event EventHandler<TestsPrioritizedEventArgs<TTestCase>> TestsPrioritized;
		Func<TTestCase, bool> FilterFunction { set; }
		Task<TResult> ExecuteRTSRun(TModel oldProgramModel, TModel newProgramModel, CancellationToken token);

		Task<TResult> ExecuteRTSRun(TDelta delta, CancellationToken token);
	}
}