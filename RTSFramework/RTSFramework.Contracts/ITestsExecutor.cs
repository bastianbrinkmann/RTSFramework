﻿using System;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;

namespace RTSFramework.Contracts
{
	public interface ITestsExecutor<TTestCase, TDelta, TModel> : ITestsProcessor<TTestCase, ITestsExecutionResult<TTestCase>, TDelta, TModel>
		where TTestCase : ITestCase
		where TDelta : IDelta<TModel>
		where TModel : IProgramModel
	{
		event EventHandler<TestCaseResultEventArgs<TTestCase>> TestResultAvailable;
	}
}