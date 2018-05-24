using System;
using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.RTSApproaches.Core
{
	public class ImpactedTestEventArgs<TTestCase> : EventArgs where TTestCase : ITestCase
	{
		public ImpactedTestEventArgs(TTestCase testCase, IList<string> responsibleChanges)
		{
			TestCase = testCase;
			ResponsibleChanges = responsibleChanges;
		}

		public TTestCase TestCase { get; private set; }

		public IList<string> ResponsibleChanges { get; private set; }
	}
}