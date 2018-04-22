using System;
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
		private DateTimeOffset startTime;
		private DateTimeOffset endTime;
		private double durationInSeconds;

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