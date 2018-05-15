using System;
using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.RTSApproaches.Core
{
	public class TestsPrioritizedEventArgs<TTestCase> : EventArgs where TTestCase : ITestCase
	{
		public TestsPrioritizedEventArgs(IList<TTestCase> testCases)
		{
			TestCases = testCases;
		}

		public IList<TTestCase> TestCases { get; private set; }
	}
}