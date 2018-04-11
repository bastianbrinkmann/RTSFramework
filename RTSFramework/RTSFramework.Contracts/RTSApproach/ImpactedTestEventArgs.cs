using System;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts.RTSApproach
{
	public class ImpactedTestEventArgs<TTestCase> : EventArgs where TTestCase : ITestCase
	{
		public ImpactedTestEventArgs(TTestCase testCase)
		{
			TestCase = testCase;
		}

		public TTestCase TestCase { get; private set; }
	}
}