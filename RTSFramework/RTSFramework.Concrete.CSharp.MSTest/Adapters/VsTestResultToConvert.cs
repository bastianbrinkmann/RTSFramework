using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using RTSFramework.Concrete.CSharp.MSTest.Models;

namespace RTSFramework.Concrete.CSharp.MSTest.Adapters
{
	public class VsTestResultToConvert
	{
		public TestResult Result { get; set; }

		public MSTestTestcase MSTestTestcase { get; set; }
	}
}