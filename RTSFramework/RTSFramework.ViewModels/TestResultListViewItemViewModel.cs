using System;
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

		public TestResultListViewItemViewModel(IDialogService dialogService)
		{
			ShowErrorMessageCommand = new DelegateCommand(() =>
			{
				if (ErrorMessage != null)
				{
					dialogService.ShowError($"Error Message: {ErrorMessage}\n\nStackTrace: {StackTrace}", "Test Run Error");
				}
			});
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
	}
}