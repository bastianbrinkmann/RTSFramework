using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.RTSApproaches.Core;

namespace RTSFramework.ViewModels.Controller
{
	public interface IController<TTestCase> where TTestCase : ITestCase
	{
		event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
		event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
		event EventHandler<TestsPrioritizedEventArgs<TTestCase>> TestsPrioritized;

		Task ExecuteImpactedTests(CancellationToken token);
	}
}