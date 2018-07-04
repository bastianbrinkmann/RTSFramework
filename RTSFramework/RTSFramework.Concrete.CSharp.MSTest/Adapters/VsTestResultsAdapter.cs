using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.TestExecution;

namespace RTSFramework.Concrete.CSharp.MSTest.Adapters
{
	public class VsTestResultsAdapter : IArtefactAdapter<VsTestResultsToConvert, IList<ITestCaseResult<MSTestTestcase>>>
	{
		private readonly IArtefactAdapter<VsTestResultToConvert, ITestCaseResult<MSTestTestcase>> testResultAdapter;

		public VsTestResultsAdapter(IArtefactAdapter<VsTestResultToConvert, ITestCaseResult<MSTestTestcase>> testResultAdapter)
		{
			this.testResultAdapter = testResultAdapter;
		}

		public IList<ITestCaseResult<MSTestTestcase>> Parse(VsTestResultsToConvert vsTestResults)
		{
			var msTestResults = new List<ITestCaseResult<MSTestTestcase>>();

			foreach (var vsTestResult in vsTestResults.Results)
			{
				var singleResult = testResultAdapter.Parse(new VsTestResultToConvert
				{
					Result = vsTestResult,
					MSTestTestcase = vsTestResults.MSTestTestcases.SingleOrDefault(x => x.VsTestTestCase.Id == vsTestResult.TestCase.Id)
				});

				if (singleResult.TestCase.IsChildTestCase)
				{
					if (msTestResults.Any(x => x.TestCase.Id == singleResult.TestCase.Id))
					{
						var compositeTestCase = (CompositeTestCaseResult<MSTestTestcase>)msTestResults.Single(x => x.TestCase.Id == singleResult.TestCase.Id);
						compositeTestCase.ChildrenResults.Add(singleResult);
					}
					else
					{
						var compositeTestCase = new CompositeTestCaseResult<MSTestTestcase>
						{
							TestCase = singleResult.TestCase
						};
						compositeTestCase.ChildrenResults.Add(singleResult);
						msTestResults.Add(compositeTestCase);
					}
				}
				else
				{
					msTestResults.Add(singleResult);
				}
			}

			return msTestResults;
		}

		public VsTestResultsToConvert Unparse(IList<ITestCaseResult<MSTestTestcase>> model, VsTestResultsToConvert artefact = null)
		{
			throw new System.NotImplementedException();
		}
	}
}