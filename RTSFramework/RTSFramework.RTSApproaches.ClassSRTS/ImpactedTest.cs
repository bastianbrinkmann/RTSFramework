using RTSFramework.Contracts.Models;

namespace RTSFramework.RTSApproaches.Static
{
	public class ImpactedTest<TTestCase> where TTestCase : ITestCase
	{
		public TTestCase TestCase { get; set; }

		public string ImpactedDueTo { get; set; }
	}
}