using System.Collections.Generic;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.Reporting
{
	public class TestListResult<TTestCase> : ITestProcessingResult where TTestCase : ITestCase
	{
		public IList<TTestCase> IdentifiedTests { get; set; }
	}
}