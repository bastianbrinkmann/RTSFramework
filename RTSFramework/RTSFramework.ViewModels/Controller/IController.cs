using System;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.RTSApproaches.Core;

namespace RTSFramework.ViewModels.Controller
{
	public interface IController<TTestCase> where TTestCase : ITestCase
	{
		event EventHandler<ImpactedTestEventArgs<TTestCase>> ImpactedTest;
		event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
	}
}