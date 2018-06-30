using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.ViewModels.Controller;
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
		private DelegateCommand showErrorMessageCommand;
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
		private DelegateCommand showResponsibleChangesCommand;
		private IList<string> responsibleChanges;

		public TestResultListViewItemViewModel(IDialogService dialogService)
		{
			ShowErrorMessageCommand = new DelegateCommand(() =>
			{
				if (ErrorMessage != null)
				{
					string message = $"Error Message: {ErrorMessage}\n\nStackTrace: {StackTrace}";
					dialogService.ShowError(message, "Test Run Error");
				}
			}, () => ErrorMessage != null);

			ShowResponsibleChangesCommand = new DelegateCommand(() =>
			{
				dialogService.ShowInformation(string.Join(Environment.NewLine, ResponsibleChanges), "Potentially responsible changes");
			}, () => ResponsibleChanges != null);

			ChildResults = new ObservableCollection<TestResultListViewItemViewModel>();

			PropertyChanged += OnPropertyChanged;
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			switch (propertyChangedEventArgs.PropertyName)
			{
				case nameof(ErrorMessage):
					ShowErrorMessageCommand.RaiseCanExecuteChanged();
					break;
				case nameof(ResponsibleChanges):
					ShowResponsibleChangesCommand.RaiseCanExecuteChanged();
					break;
			}
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

		public IList<string> ResponsibleChanges
		{
			get { return responsibleChanges; }
			set
			{
				responsibleChanges = value;
				RaisePropertyChanged();
			}
		}

		public DelegateCommand ShowResponsibleChangesCommand
		{
			get
			{
				return showResponsibleChangesCommand;
			}
			set
			{
				showResponsibleChangesCommand = value;
				RaisePropertyChanged();
			}
		}

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

		public DelegateCommand ShowErrorMessageCommand
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