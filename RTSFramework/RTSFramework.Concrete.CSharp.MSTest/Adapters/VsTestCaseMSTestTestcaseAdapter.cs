using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Contracts.Adapter;

namespace RTSFramework.Concrete.CSharp.MSTest.Adapters
{
	public class VsTestCaseMSTestTestcaseAdapter : IArtefactAdapter<TestCase, MSTestTestcase>
	{
		public MSTestTestcase Parse(TestCase vsTestCase)
		{
			var testNameProperty = vsTestCase.Properties.SingleOrDefault(x => x.Id == MSTestConstants.PropertyTestClassName);
			var isEnabledProperty = vsTestCase.Properties.SingleOrDefault(x => x.Id == MSTestConstants.PropertyIsEnabled);
			var testCategoryProperty = vsTestCase.Properties.SingleOrDefault(x => x.Id == MSTestConstants.PropertyTestCategory);
			var dataDrivenProperty = vsTestCase.Properties.SingleOrDefault(x => x.Id == MSTestConstants.PropertyIsDataDriven);

			string testClassName = testNameProperty != null ? vsTestCase.GetPropertyValue(testNameProperty, "") : "";
			bool isEnabled = isEnabledProperty == null || vsTestCase.GetPropertyValue(isEnabledProperty, true);
			string[] categories = testCategoryProperty != null ? vsTestCase.GetPropertyValue(testCategoryProperty, new string[0]) : new string[0];
			bool isDataDriven = dataDrivenProperty != null && vsTestCase.GetPropertyValue(dataDrivenProperty, false);

			var msTestCase = new MSTestTestcase
			{
				Name = vsTestCase.DisplayName,
				AssemblyPath = vsTestCase.Source,
				Id = vsTestCase.FullyQualifiedName,
				AssociatedClasses = new List<string> { testClassName },
				Ignored = !isEnabled,
				VsTestTestCase = vsTestCase,
				IsChildTestCase = isDataDriven
			};
			msTestCase.Categories.AddRange(categories);

			return msTestCase;
		}

		public TestCase Unparse(MSTestTestcase model, TestCase artefact = null)
		{
			throw new System.NotImplementedException();
		}
	}
}