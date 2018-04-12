using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Concrete.Reporting;
using RTSFramework.Concrete.TFS2010.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.ViewModels.RequireUIServices;
using RTSFramework.ViewModels.RunConfigurations;

namespace RTSFramework.ViewModels
{
	public class MainWindowViewModel : BindableBase
	{
		private readonly IDialogService dialogService;

		private string result;
		private ICommand startRunCommand;
		private ProcessingType processingType;
		private DiscoveryType discoveryType;
		private RTSApproachType rtsApproachType;
		private GranularityLevel granularityLevel;
		private bool isGranularityLevelChangable;
		private string solutionFilePath;
		private string gitRepositoryPath;
		private bool isGitRepositoryPathChangable;
		private ProgramModelType programModelType;
		private bool isRunning;
		private ICommand cancelRunCommand;

		public MainWindowViewModel(IDialogService dialogService)
		{
			this.dialogService = dialogService;

			StartRunCommand = new DelegateCommand(ExecuteRunFixModel);
			CancelRunCommand = new DelegateCommand(CancelRun);
			TestResults = new ObservableCollection<TestResultListViewItemViewModel>();
			RunStatus = RunStatus.Ready;

			//Defaults
			DiscoveryType = DiscoveryType.LocalDiscovery;
			ProcessingType = ProcessingType.MSTestExecution;
			RTSApproachType = RTSApproachType.ClassSRTS;
			GranularityLevel = GranularityLevel.Class;
			IsGranularityLevelChangable = false;
			SolutionFilePath = @"C:\Git\TIATestProject\TIATestProject.sln";
			ProgramModelType = ProgramModelType.GitProgramModel;
			GitRepositoryPath = @"C:\Git\TIATestProject\";
			IsGitRepositoryPathChangable = true;

			PropertyChanged += OnPropertyChanged;
		}

		private CancellationTokenSource cancellationTokenSource;
		private ObservableCollection<TestResultListViewItemViewModel> testResults;
		private RunStatus runStatus;

		private void CancelRun()
		{
			cancellationTokenSource?.Cancel();
			RunStatus = RunStatus.Cancelled;
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			if (propertyChangedEventArgs.PropertyName == nameof(RTSApproachType))
			{
				if (RTSApproachType == RTSApproachType.ClassSRTS)
				{
					GranularityLevel = GranularityLevel.Class;
				}
				IsGranularityLevelChangable = RTSApproachType == RTSApproachType.DynamicRTS;
			}

			if (propertyChangedEventArgs.PropertyName == nameof(ProgramModelType))
			{
				IsGitRepositoryPathChangable = ProgramModelType == ProgramModelType.GitProgramModel;
			}

			if (propertyChangedEventArgs.PropertyName == nameof(RunStatus))
			{
				IsRunning = RunStatus == RunStatus.Running;
			}
		}

		#region Properties

		public ObservableCollection<TestResultListViewItemViewModel> TestResults
		{
			get { return testResults; }
			set
			{
				testResults = value;
				RaisePropertyChanged();
			}
		}

		public ICommand CancelRunCommand
		{
			get { return cancelRunCommand; }
			set
			{
				cancelRunCommand = value;
				RaisePropertyChanged();
			}
		}

		public RunStatus RunStatus
		{
			get { return runStatus; }
			set
			{
				runStatus = value;
				RaisePropertyChanged();
			}
		}

		public bool IsRunning
		{
			get { return isRunning; }
			set
			{
				isRunning = value;
				RaisePropertyChanged();
			}
		}

		public ProgramModelType ProgramModelType
		{
			get { return programModelType; }
			set
			{
				programModelType = value;
				RaisePropertyChanged();
			}
		}

		public bool IsGitRepositoryPathChangable
		{
			get { return isGitRepositoryPathChangable; }
			set
			{
				isGitRepositoryPathChangable = value;
				RaisePropertyChanged();
			}
		}

		public string GitRepositoryPath
		{
			get { return gitRepositoryPath; }
			set
			{
				gitRepositoryPath = value;
				RaisePropertyChanged();
			}
		}

		public string SolutionFilePath
		{
			get { return solutionFilePath; }
			set
			{
				solutionFilePath = value;
				RaisePropertyChanged();
			}
		}

		public bool IsGranularityLevelChangable
		{
			get { return isGranularityLevelChangable; }
			set
			{
				isGranularityLevelChangable = value;
				RaisePropertyChanged();
			}
		}

		public GranularityLevel GranularityLevel
		{
			get { return granularityLevel; }
			set
			{
				granularityLevel = value;
				RaisePropertyChanged();
			}
		}

		public RTSApproachType RTSApproachType
		{
			get { return rtsApproachType; }
			set
			{
				rtsApproachType = value;
				RaisePropertyChanged();
			}
		}

		public DiscoveryType DiscoveryType
		{
			get { return discoveryType; }
			set
			{
				discoveryType = value;
				RaisePropertyChanged();
			}
		}

		public ProcessingType ProcessingType
		{
			get { return processingType; }
			set
			{
				processingType = value;
				RaisePropertyChanged();
			}
		}

		public string Result
		{
			get { return result; }
			set
			{
				result = value;
				RaisePropertyChanged();
			}
		}

		public ICommand StartRunCommand
		{
			get { return startRunCommand; }
			set
			{
				startRunCommand = value;
				RaisePropertyChanged();
			}
		}

		#endregion

		private async void ExecuteRunFixModel()
		{
			RunStatus = RunStatus.Running;
			cancellationTokenSource = new CancellationTokenSource();

			try
			{
				switch (ProgramModelType)
				{
					case ProgramModelType.GitProgramModel:
						var oldGitModel = GitProgramModelProvider.GetGitProgramModel(GitRepositoryPath, GitVersionReferenceType.LatestCommit);
						var newGitModel = GitProgramModelProvider.GetGitProgramModel(GitRepositoryPath, GitVersionReferenceType.CurrentChanges);

						await ExecuteRunFixGranularityLevel(oldGitModel, newGitModel);
						break;
					case ProgramModelType.TFS2010ProgramModel:
						if (DiscoveryType == DiscoveryType.LocalDiscovery)
						{
							dialogService.ShowError("Local Discovery combined with TFS 2010 is not supported yet!");
							return;
						}

						var oldTfsModel = new TFS2010ProgramModel { VersionId = "Test" };
						var newTfsModel = new TFS2010ProgramModel { VersionId = "Test2" };

						await ExecuteRunFixGranularityLevel(oldTfsModel, newTfsModel);
						break;
				}

				RunStatus = RunStatus.Completed;
			}
			catch (Exception e)
			{
				dialogService.ShowError(e.Message);
			}
		}

		private async Task ExecuteRunFixGranularityLevel<TModel>(TModel oldProgramModel, TModel newProgramModel)
			where TModel : CSharpProgramModel
		{
			oldProgramModel.AbsoluteSolutionPath = SolutionFilePath;
			newProgramModel.AbsoluteSolutionPath = SolutionFilePath;
			oldProgramModel.GranularityLevel = GranularityLevel;
			newProgramModel.GranularityLevel = GranularityLevel;

			switch (GranularityLevel)
			{
				case GranularityLevel.File:
					await ExecuteRunFixProcessingType<TModel, StructuralDelta<TModel, CSharpFileElement>>(oldProgramModel, newProgramModel);
					break;
				case GranularityLevel.Class:
					await ExecuteRunFixProcessingType<TModel, StructuralDelta<TModel, CSharpClassElement>>(oldProgramModel, newProgramModel);
					break;
			}
		}

		private async Task ExecuteRunFixProcessingType<TModel, TDelta>(TModel oldProgramModel, TModel newProgramModel)
			where TModel : IProgramModel
			where TDelta : IDelta
		{
			switch (ProcessingType)
			{
				case ProcessingType.MSTestExecution:
				case ProcessingType.MSTestExecutionWithCoverage:
					var executionResult = await ExecuteRun<TModel, TDelta, MSTestTestcase, MSTestExectionResult>(oldProgramModel, newProgramModel);

					TestResults.Clear();
					TestResults.AddRange(executionResult.TestcasesResults.Select(x => new TestResultListViewItemViewModel
					{
						FullyQualifiedName = x.TestCaseId,
						TestOutcome = x.Outcome
					}));
					break;
				case ProcessingType.CsvReporting:
					var csvCreationResult = await ExecuteRun<TModel, TDelta, MSTestTestcase, FileProcessingResult>(oldProgramModel, newProgramModel);
					bool openFile = dialogService.ShowQuestion($"CSV file was created at '{csvCreationResult.FilePath}'.{Environment.NewLine} Do you want to open the file?","CSV File Created");
					if (openFile)
					{
						Process.Start(csvCreationResult.FilePath);
					}

					break;
				case ProcessingType.ListReporting:
					var listReportingResult = await ExecuteRun<TModel, TDelta, MSTestTestcase, TestListResult<MSTestTestcase>>(oldProgramModel, newProgramModel);
					TestResults.Clear();
					TestResults.AddRange(listReportingResult.IdentifiedTests.Select(x => new TestResultListViewItemViewModel
					{
						FullyQualifiedName = x.Id,
						Categories = string.Join(",", x.Categories)
					}));
					break;
			}
		}

		private async Task<TResult> ExecuteRun<TModel, TDelta, TTestCase, TResult>(TModel oldProgramModel, TModel newProgramModel) where TTestCase : ITestCase
			where TModel : IProgramModel
			where TDelta : IDelta
			where TResult : ITestProcessingResult
		{
			var stateBasedController = UnityModelInitializer.GetStateBasedController<TModel, TDelta, TTestCase, TResult>(DiscoveryType, RTSApproachType, ProcessingType);

			return
				await Task.Run(
					() => stateBasedController.ExecuteImpactedTests(oldProgramModel, newProgramModel, cancellationTokenSource.Token),
					cancellationTokenSource.Token);
		}
	}
}