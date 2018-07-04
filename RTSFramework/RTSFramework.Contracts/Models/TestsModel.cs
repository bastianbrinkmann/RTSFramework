using System.Collections.Generic;

namespace RTSFramework.Contracts.Models
{
	public class TestsModel<TTestCase> where TTestCase : ITestCase
	{
		public string VersionId { get; set; }

		public ISet<TTestCase> TestSuite { get; set; }
	}
}