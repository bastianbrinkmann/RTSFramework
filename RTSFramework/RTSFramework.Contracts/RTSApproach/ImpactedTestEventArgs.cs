using System;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts.RTSApproach
{
	public class ImpactedTestEventArgs<TTc> : EventArgs where TTc : ITestCase
	{
		public ImpactedTestEventArgs(TTc testCase)
		{
			TestCase = testCase;
		}

		public TTc TestCase { get; private set; }
	}
}