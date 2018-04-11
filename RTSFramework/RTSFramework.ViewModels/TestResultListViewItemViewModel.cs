using Prism.Mvvm;

namespace RTSFramework.ViewModels
{
	public class TestResultListViewItemViewModel : BindableBase
	{
		private string fullyQualifiedName;
		private string testOutcome;

		public string FullyQualifiedName
		{
			get { return fullyQualifiedName; }
			set
			{
				fullyQualifiedName = value;
				RaisePropertyChanged();
			}
		}

		//TODO: TestOutcome as Type and ValueConverter
		public string TestOutcome
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