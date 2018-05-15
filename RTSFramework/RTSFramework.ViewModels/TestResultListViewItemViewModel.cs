using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.ViewModels.RequireUIServices;

namespace RTSFramework.ViewModels
{
	public class TestResultListViewItemViewModel : BindableBase
	{
		private TestExecutionOutcome testOutcome;
		private string categories;
		private DateTimeOffset startTime;
		private DateTimeOffset endTime;
		private double durationInSeconds;
		private ICommand showErrorMessageCommand;
		private string errorMessage;
		private string stackTrace;
		private string name;
		private string fullClassName;
		private string fullyQualifiedName;
		private bool hasChildResults;
		private bool areChildResultsShown;
		private ObservableCollection<TestResultListViewItemViewModel> childResults;
		private string displayName;
		private int? executionId;

		public TestResultListViewItemViewModel(IDialogService dialogService)
		{
			ShowErrorMessageCommand = new DelegateCommand(() =>
			{
				if (ErrorMessage != null)
				{
					dialogService.ShowError($"Error Message: {ErrorMessage}\n\nStackTrace: {StackTrace}", "Test Run Error");
				}
			});

			ChildResults = new ObservableCollection<TestResultListViewItemViewModel>();
		}

		public void AddChildResults(TestResultListViewItemViewModel childResult)
		{
			ChildResults.Add(childResult);

			StartTime = childResults.Min(x => x.StartTime);
			EndTime = childResults.Max(x => x.EndTime);
			DurationInSeconds = childResults.Sum(x => x.DurationInSeconds);
			TestOutcome = childResults.All(x => x.TestOutcome == TestExecutionOutcome.Passed) ? TestExecutionOutcome.Passed : TestExecutionOutcome.Failed;
		}

		#region Properties

		public int? ExecutionId
		{
			get { return executionId; }
			set
			{
				executionId = value;
				RaisePropertyChanged();
			}
		}

		public ObservableCollection<TestResultListViewItemViewModel> ChildResults
		{
			get { return childResults; }
			set
			{
				childResults = value;
				RaisePropertyChanged();
			}
		}

		public bool AreChildResultsShown
		{
			get { return areChildResultsShown; }
			set
			{
				areChildResultsShown = value;
				RaisePropertyChanged();
			}
		}

		public bool HasChildResults
		{
			get { return hasChildResults; }
			set
			{
				hasChildResults = value;
				RaisePropertyChanged();
			}
		}

		public string FullClassName
		{
			get { return fullClassName; }
			set
			{
				fullClassName = value;
				RaisePropertyChanged();
			}
		}

		public string Name
		{
			get { return name; }
			set
			{
				name = value;
				RaisePropertyChanged();
			}
		}

		public string StackTrace
		{
			get { return stackTrace; }
			set
			{
				stackTrace = value;
				RaisePropertyChanged();
			}
		}

		public string ErrorMessage
		{
			get { return errorMessage; }
			set
			{
				errorMessage = value;
				RaisePropertyChanged();
			}
		}

		public ICommand ShowErrorMessageCommand
		{
			get { return showErrorMessageCommand; }
			set
			{
				showErrorMessageCommand = value;
				RaisePropertyChanged();
			}
		}

		public double DurationInSeconds
		{
			get { return durationInSeconds; }
			set
			{
				durationInSeconds = value;
				RaisePropertyChanged();
			}
		}

		public DateTimeOffset EndTime
		{
			get { return endTime; }
			set
			{
				endTime = value;
				RaisePropertyChanged();
			}
		}

		public DateTimeOffset StartTime
		{
			get { return startTime; }
			set
			{
				startTime = value;
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

		public string FullyQualifiedName
		{
			get { return fullyQualifiedName; }
			set
			{
				fullyQualifiedName = value;
				RaisePropertyChanged();
			}
		}

		public string DisplayName
		{
			get { return displayName; }
			set
			{
				displayName = value;
				RaisePropertyChanged();
			}
		}

		#endregion
	}
}