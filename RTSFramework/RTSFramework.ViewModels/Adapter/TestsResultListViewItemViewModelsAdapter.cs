using System.Collections.Generic;
using System.Linq;
using RTSFramework.Concrete.Reporting;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;

namespace RTSFramework.ViewModels.Adapter
{
	public class TestsResultListViewItemViewModelsAdapter<TTestCase> : IArtefactAdapter<IList<TestResultListViewItemViewModel>, TestListResult<TTestCase>> 
		where TTestCase : ITestCase 
	{
		private readonly IArtefactAdapter<TestResultListViewItemViewModel, TTestCase> singleItemAdapter;

		public TestsResultListViewItemViewModelsAdapter(IArtefactAdapter<TestResultListViewItemViewModel, TTestCase> singleItemAdapter)
		{
			this.singleItemAdapter = singleItemAdapter;
		}

		public TestListResult<TTestCase> Parse(IList<TestResultListViewItemViewModel> artefact)
		{
			throw new System.NotImplementedException();
		}

		public IList<TestResultListViewItemViewModel> Unparse(TestListResult<TTestCase> model, IList<TestResultListViewItemViewModel> artefact)
		{
			return model.IdentifiedTests.Select(x =>
			{
				var viewModel = singleItemAdapter.Unparse(x);
				viewModel.ExecutionId = model.IdentifiedTests.IndexOf(x);
				return viewModel;
			}).ToList();
		}
	}
}