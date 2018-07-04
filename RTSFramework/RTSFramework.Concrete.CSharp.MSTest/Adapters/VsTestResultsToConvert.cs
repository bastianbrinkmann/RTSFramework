using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using RTSFramework.Concrete.CSharp.MSTest.Models;

namespace RTSFramework.Concrete.CSharp.MSTest.Adapters
{
	public class VsTestResultsToConvert
	{
		public IList<TestResult> Results { get; set; }

		public IList<MSTestTestcase> MSTestTestcases { get; set; }
	}
}