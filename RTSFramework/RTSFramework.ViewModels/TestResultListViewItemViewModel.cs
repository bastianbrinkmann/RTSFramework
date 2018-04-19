using Prism.Mvvm;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.TestExecution;

namespace RTSFramework.ViewModels
{
	public class TestResultListViewItemViewModel : BindableBase
	{
		private string fullyQualifiedName;
		private TestExecutionOutcome testOutcome;
		private string categories;

		public string FullyQualifiedName
		{
			get { return fullyQualifiedName; }
			set
			{
				fullyQualifiedName = value;
				RaisePropertyChanged();
			}
		}

		public string Categories
		{
			get { return categories; }
			set
			{
				categories = value;
				RaisePropertyChanged();
			}
		}

		public TestExecutionOutcome TestOutcome
		{
			get { return testOutcome; }
			set
			{
				testOutcome = value;
				RaisePropertyChanged();
			}
		}
	}
}