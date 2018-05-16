using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.ViewModels.RequireUIServices;

namespace RTSFramework.ViewModels.Adapter
{
	public class TestResultListViewItemViewModelAdapter<TTestCase> : IArtefactAdapter<TestResultListViewItemViewModel, TTestCase> 
		where TTestCase : ITestCase 
	{
		private readonly IDialogService dialogService;

		public TestResultListViewItemViewModelAdapter(IDialogService dialogService)
		{
			this.dialogService = dialogService;
		}

		public TTestCase Parse(TestResultListViewItemViewModel artefact)
		{
			throw new System.NotImplementedException();
		}

		public TestResultListViewItemViewModel Unparse(TTestCase model, TestResultListViewItemViewModel artefact)
		{
			if (artefact == null)
			{
				artefact = new TestResultListViewItemViewModel(dialogService);
			}

			artefact.FullyQualifiedName = model.Id;
			artefact.FullClassName = model.AssociatedClass;
			artefact.Name = model.Name;
			artefact.Categories = string.Join(",", model.Categories);

			return artefact;
		}
	}
}