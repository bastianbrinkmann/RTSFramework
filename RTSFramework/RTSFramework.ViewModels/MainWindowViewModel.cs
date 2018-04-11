using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using RTSFramework.ViewModels.RunConfigurations;
using RTSFramework.ViewModels.Utilities;

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

			StartRunCommand = new DelegateCommand(StartRun);
			CancelRunCommand = new DelegateCommand(CancelRun);
			TestResults = new ObservableCollection<TestResultListViewItemViewModel>();
			IsRunning = false;

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

		private void CancelRun()
		{
			cancellationTokenSource?.Cancel();
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

		private async void StartRun()
		{
			IsRunning = true;
			cancellationTokenSource = new CancellationTokenSource();

			try
			{
				if (ProgramModelType == ProgramModelType.GitProgramModel)
				{
					var oldProgramModel = GitProgramModelProvider.GetGitProgramModel(GitRepositoryPath, GitVersionReferenceType.LatestCommit);
					var newProgramModel = GitProgramModelProvider.GetGitProgramModel(GitRepositoryPath, GitVersionReferenceType.CurrentChanges);

					await ExecuteRunForModel(oldProgramModel, newProgramModel);
				}
				else if(ProgramModelType == ProgramModelType.TFS2010ProgramModel)
				{
					if (DiscoveryType == DiscoveryType.LocalDiscovery)
					{
						dialogService.ShowError("Local Discovery combined with TFS 2010 is not supported yet!");
						return;
					}

					var oldProgramModel = new TFS2010ProgramModel { VersionId = "Test" };
					var newProgramModel = new TFS2010ProgramModel { VersionId = "Test2" };

					await ExecuteRunForModel(oldProgramModel, newProgramModel);
				}
			}
			catch (Exception e)
			{
				dialogService.ShowError(e.Message);
			}
			finally
			{
				IsRunning = false;
			}
		}

		private async Task ExecuteRunForModel<TModel>(TModel oldProgramModel, TModel newProgramModel)
			where TModel : CSharpProgramModel
		{
			var configuration = new RunConfiguration<TModel>();
			SetConfig(configuration, oldProgramModel, newProgramModel);

			if (configuration.GranularityLevel == GranularityLevel.File)
			{
				await ExecuteRunOnGranularityLevel<TModel, StructuralDelta<TModel, CSharpFileElement>>(configuration);
			}
			else if (configuration.GranularityLevel == GranularityLevel.Class)
			{
				await ExecuteRunOnGranularityLevel<TModel, StructuralDelta<TModel, CSharpClassElement>>(configuration);
			}
		}

		private void SetConfig<TModel>(RunConfiguration<TModel> configuration, TModel oldProgramModel, TModel newProgramModel) where TModel : CSharpProgramModel
		{
			configuration.ProcessingType = ProcessingType;
			configuration.DiscoveryType = DiscoveryType;
			configuration.GitRepositoryPath = GitRepositoryPath;
			configuration.AbsoluteSolutionPath = SolutionFilePath;
			configuration.RTSApproachType = RTSApproachType;
			configuration.GranularityLevel = GranularityLevel;

			oldProgramModel.AbsoluteSolutionPath = configuration.AbsoluteSolutionPath;
			newProgramModel.AbsoluteSolutionPath = configuration.AbsoluteSolutionPath;
			oldProgramModel.GranularityLevel = GranularityLevel;
			newProgramModel.GranularityLevel = GranularityLevel;

			configuration.OldProgramModel = oldProgramModel;
			configuration.NewProgramModel = newProgramModel;
		}

		private async Task ExecuteRunOnGranularityLevel<TModel, TDelta>(RunConfiguration<TModel> configuration)
			where TModel : IProgramModel
			where TDelta : IDelta
		{
			switch (configuration.ProcessingType)
			{
				case ProcessingType.MSTestExecution:
				case ProcessingType.MSTestExecutionWithCoverage:
					var executionResult = await ExecuteRun<TModel, TDelta, MSTestTestcase, MSTestExectionResult>(configuration);

					TestResults.Clear();
					TestResults.AddRange(executionResult.TestcasesResults.Select(x => new TestResultListViewItemViewModel
					{
						FullyQualifiedName = x.TestCaseId,
						TestOutcome = x.Outcome.ToString()
					}));
					break;
				case ProcessingType.CsvReporting:
					var csvCreationResult = await ExecuteRun<TModel, TDelta, MSTestTestcase, FileProcessingResult>(configuration);

					break;
				case ProcessingType.ListReporting:
					var listReportingResult = await ExecuteRun<TModel, TDelta, MSTestTestcase, TestListResult<MSTestTestcase>>(configuration);
					TestResults.Clear();
					TestResults.AddRange(listReportingResult.IdentifiedTests.Select(x => new TestResultListViewItemViewModel
					{
						FullyQualifiedName = x.Id
					}));
					break;
			}
		}

		private async Task<TResult> ExecuteRun<TModel, TDelta, TTestCase, TResult>(RunConfiguration<TModel> configuration) where TTestCase : ITestCase
			where TModel : IProgramModel
			where TDelta : IDelta
			where TResult : ITestProcessingResult
		{
			var stateBasedController = UnityModelInitializer.GetStateBasedController<TModel, TDelta, TTestCase, TResult>();

			return await Task.Run(() => stateBasedController.ExecuteImpactedTests(configuration, cancellationTokenSource.Token), cancellationTokenSource.Token);
		}
	}
}