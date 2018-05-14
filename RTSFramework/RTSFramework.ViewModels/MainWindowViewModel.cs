using System;
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
using RTSFramework.Concrete.CSharp.MSTest;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Concrete.Reporting;
using RTSFramework.Concrete.TFS2010;
using RTSFramework.Concrete.TFS2010.Models;
using RTSFramework.Concrete.User;
using RTSFramework.Concrete.User.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.Contracts.Utilities;
using RTSFramework.Core;
using RTSFramework.RTSApproaches.Core;
using RTSFramework.RTSApproaches.Dynamic;
using RTSFramework.ViewModels.RequireUIServices;
using RTSFramework.ViewModels.RunConfigurations;

namespace RTSFramework.ViewModels
{
	public class MainWindowViewModel : BindableBase
	{
		private const string UncommittedChangesIdentifier = "uncomittedChanges";

		private readonly IDialogService dialogService;
		private readonly IApplicationUiExecutor applicationUiExecutor;
		private readonly IUserRunConfigurationProvider userRunConfigurationProvider;
		private readonly GitCommitsProvider gitCommitsProvider;
		private CancellationTokenSource cancellationTokenSource;

		#region BackingFields

		private ICommand startRunCommand;
		private ProcessingType processingType;
		private DiscoveryType discoveryType;
		private RTSApproachType rtsApproachType;
		private GranularityLevel granularityLevel;
		private bool isGranularityLevelChangable;
		private string solutionFilePath;
		private string repositoryPath;
		private bool isRunning;
		private ICommand cancelRunCommand;
		private ObservableCollection<TestResultListViewItemViewModel> testResults;
		private RunStatus runStatus;
		private ICommand selectSolutionFileCommand;
		private ICommand selectRepositoryCommand;
		private ICommand specitfyIntendedChangesCommand;
		private bool isIntededChangesEditingEnabled;
		private CommitViewModel fromCommit;
		private CommitViewModel toCommit;
		private ObservableCollection<CommitViewModel> fromCommitModels;
		private ObservableCollection<CommitViewModel> toCommitModels;
		private bool isFromCommitChangeable;
		private bool isToCommitChangeable;
		private ProgramModelType programModelType;
		private bool isRepositoryPathChangable;
		private ObservableCollection<DiscoveryType> discoveryTypes;
		private bool isTimeLimitChangeable;
		private double timeLimit;

		#endregion

		public MainWindowViewModel(IDialogService dialogService, 
			GitCommitsProvider gitCommitsProvider, 
			IApplicationUiExecutor applicationUiExecutor,
			IUserRunConfigurationProvider userRunConfigurationProvider)
		{
			this.dialogService = dialogService;
			this.applicationUiExecutor = applicationUiExecutor;
			this.userRunConfigurationProvider = userRunConfigurationProvider;
			this.gitCommitsProvider = gitCommitsProvider;

			StartRunCommand = new DelegateCommand(ExecuteRunFixModel);
			CancelRunCommand = new DelegateCommand(CancelRun);
			SelectSolutionFileCommand = new DelegateCommand(SelectSolutionFile);
			SelectRepositoryCommand = new DelegateCommand(SelectRepository);
			SpecitfyIntendedChangesCommand = new DelegateCommand(SpecifyIntendedChanges);

			DiscoveryTypes = new ObservableCollection<DiscoveryType>();
			TestResults = new ObservableCollection<TestResultListViewItemViewModel>();
			FromCommitModels = new ObservableCollection<CommitViewModel>();
			ToCommitModels = new ObservableCollection<CommitViewModel>();

			RunStatus = RunStatus.Ready;

			PropertyChanged += OnPropertyChanged;

			//TODO: Defaults - Load from config
			ProgramModelType = ProgramModelType.GitModel;
			DiscoveryType = DiscoveryType.GitDiscovery;
			ProcessingType = ProcessingType.MSTestExecution;
			RTSApproachType = RTSApproachType.ClassSRTS;
			GranularityLevel = GranularityLevel.Class;
			IsGranularityLevelChangable = false;
			SolutionFilePath = @"C:\Git\TIATestProject\TIATestProject.sln";
			RepositoryPath = @"C:\Git\TIATestProject\";
		}

		private void SpecifyIntendedChanges()
		{
			var intendedChangesViewModel = dialogService.OpenDialogByViewModel<IntendedChangesDialogViewModel>();

			intendedChangesViewModel.RootDirectory = Path.GetDirectoryName(SolutionFilePath);
		}

		private void SelectRepository()
		{
			string selectedDirectory;
			if (dialogService.SelectDirectory(RepositoryPath, out selectedDirectory))
			{
				RepositoryPath = selectedDirectory;
			}
		}

		private void SelectSolutionFile()
		{
			string selectedFile;
			if (dialogService.SelectFile(RepositoryPath, "Solution Files (*.sln)|*.sln", out selectedFile))
			{
				SolutionFilePath = selectedFile;
			}
		}

		private void CancelRun()
		{
			cancellationTokenSource?.Cancel();
		}

		private void RefreshCommitsSelection()
		{
			FromCommitModels.Clear();
			FromCommitModels.AddRange(gitCommitsProvider.GetAllCommits(RepositoryPath).Select(ConvertCommit));
			FromCommit = FromCommitModels.FirstOrDefault();
			IsFromCommitChangeable = DiscoveryType == DiscoveryType.GitDiscovery && FromCommitModels.Any();
			IsToCommitChangeable = DiscoveryType == DiscoveryType.GitDiscovery && ToCommitModels.Any();
		}

		private CommitViewModel ConvertCommit(GitCommit gitCommit)
		{
			return new CommitViewModel
			{
				Committer = gitCommit.Committer,
				Identifier = gitCommit.ShaId,
				Message = gitCommit.Message
			};
		}

		private void RefreshDiscoveryTypes()
		{
			if (ProgramModelType == ProgramModelType.GitModel)
			{
				DiscoveryTypes.Clear();
				DiscoveryTypes.Add(DiscoveryType.GitDiscovery);
				DiscoveryTypes.Add(DiscoveryType.UserIntendedChangesDiscovery);
			}
			else if (ProgramModelType == ProgramModelType.TFS2010Model)
			{
				DiscoveryTypes.Clear();
				DiscoveryTypes.Add(DiscoveryType.UserIntendedChangesDiscovery);
			}
			DiscoveryType = DiscoveryTypes.FirstOrDefault();
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			switch (propertyChangedEventArgs.PropertyName)
			{
				case nameof(ProcessingType):
					IsTimeLimitChangeable = ProcessingType == ProcessingType.MSTestExecutionLimitedTime;
					break;
				case nameof(RTSApproachType):
					/*TODO Granularity Level File
					 * 
					 * if (RTSApproachType == RTSApproachType.ClassSRTS)
					{
						GranularityLevel = GranularityLevel.Class;
					}
					IsGranularityLevelChangable = RTSApproachType == RTSApproachType.DynamicRTS;*/
					break;
				case nameof(ProgramModelType):
					IsRepositoryPathChangable = ProgramModelType == ProgramModelType.GitModel;
					RefreshDiscoveryTypes();
					break;
				case nameof(DiscoveryType):
					IsIntededChangesEditingEnabled = DiscoveryType == DiscoveryType.UserIntendedChangesDiscovery;
					IsFromCommitChangeable = DiscoveryType == DiscoveryType.GitDiscovery && FromCommitModels.Any();
					IsToCommitChangeable = DiscoveryType == DiscoveryType.GitDiscovery && ToCommitModels.Any();
					break;
				case nameof(RunStatus):
					IsRunning = RunStatus == RunStatus.Running;
					break;
				case nameof(RepositoryPath):
					RefreshCommitsSelection();
					break;
				case nameof(FromCommit):
					var toCommitId = ToCommit?.Identifier;
					ToCommitModels.Clear();
					ToCommitModels.Add(new CommitViewModel
					{
						DisplayName = "Uncommitted Changes",
						Identifier = UncommittedChangesIdentifier
					});
					ToCommitModels.AddRange(gitCommitsProvider.GetAllCommits(RepositoryPath).TakeWhile(x => x.ShaId != FromCommit.Identifier).Select(ConvertCommit));
					ToCommit = ToCommitModels.SingleOrDefault(x => x.Identifier == toCommitId) ?? ToCommitModels.FirstOrDefault();
					break;
				case nameof(TimeLimit):
					userRunConfigurationProvider.TimeLimit = TimeLimit;
					break;
			}
		}

		#region Properties

		public double TimeLimit
		{
			get { return timeLimit; }
			set
			{
				timeLimit = value;
				RaisePropertyChanged();
			}
		}

		public bool IsTimeLimitChangeable
		{
			get { return isTimeLimitChangeable; }
			set
			{
				isTimeLimitChangeable = value;
				RaisePropertyChanged();
			}
		}

		public ObservableCollection<DiscoveryType> DiscoveryTypes
		{
			get { return discoveryTypes; }
			set
			{
				discoveryTypes = value;
				RaisePropertyChanged();
			}
		}

		public bool IsRepositoryPathChangable
		{
			get { return isRepositoryPathChangable; }
			set
			{
				isRepositoryPathChangable = value;
				RaisePropertyChanged();
			}
		}

		public bool IsToCommitChangeable
		{
			get { return isToCommitChangeable; }
			set
			{
				isToCommitChangeable = value;
				RaisePropertyChanged();
			}
		}

		public bool IsFromCommitChangeable
		{
			get { return isFromCommitChangeable; }
			set
			{
				isFromCommitChangeable = value;
				RaisePropertyChanged();
			}
		}

		public ObservableCollection<CommitViewModel> ToCommitModels
		{
			get { return toCommitModels; }
			set
			{
				toCommitModels = value;
				RaisePropertyChanged();
			}
		}

		public ObservableCollection<CommitViewModel> FromCommitModels
		{
			get { return fromCommitModels; }
			set
			{
				fromCommitModels = value;
				RaisePropertyChanged();
			}
		}

		public CommitViewModel ToCommit
		{
			get { return toCommit; }
			set
			{
				toCommit = value;
				RaisePropertyChanged();
			}
		}

		public CommitViewModel FromCommit
		{
			get { return fromCommit; }
			set
			{
				fromCommit = value;
				RaisePropertyChanged();
			}
		}

		public bool IsIntededChangesEditingEnabled
		{
			get { return isIntededChangesEditingEnabled; }
			set
			{
				isIntededChangesEditingEnabled = value;
				RaisePropertyChanged();
			}
		}

		public ICommand SpecitfyIntendedChangesCommand
		{
			get { return specitfyIntendedChangesCommand; }
			set
			{
				specitfyIntendedChangesCommand = value;
				RaisePropertyChanged();
			}
		}

		public ICommand SelectRepositoryCommand
		{
			get { return selectRepositoryCommand; }
			set
			{
				selectRepositoryCommand = value;
				RaisePropertyChanged();
			}
		}

		public ICommand SelectSolutionFileCommand
		{
			get { return selectSolutionFileCommand; }
			set
			{
				selectSolutionFileCommand = value;
				RaisePropertyChanged();
			}
		}

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

		public string RepositoryPath
		{
			get { return repositoryPath; }
			set
			{
				repositoryPath = value;
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

		public ICommand StartRunCommand
		{
			get { return startRunCommand; }
			set
			{
				startRunCommand = value;
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

		#endregion

		private async void ExecuteRunFixModel()
		{
			RunStatus = RunStatus.Running;
			TestResults.Clear();

			cancellationTokenSource = new CancellationTokenSource();

			try
			{
				switch (DiscoveryType)
				{
					case DiscoveryType.GitDiscovery:
						await ExecuteGitRun();
						break;
					case DiscoveryType.UserIntendedChangesDiscovery:
						await ExecuteUserIntendedChangesRun();
						break;
				}

				RunStatus = RunStatus.Completed;
			}
			catch (TerminationConditionReachedException e)
			{
				RunStatus = RunStatus.Completed;
				dialogService.ShowInformation($"Execution stopped after {TimeLimit} seconds.");
			}
			catch (Exception e)
			{
				if (cancellationTokenSource.IsCancellationRequested)
				{
					RunStatus = RunStatus.Cancelled;
					return;
				}
				RunStatus = RunStatus.Failed;
				dialogService.ShowError(e.Message);
			}
		}

		#region DeltaBasedController

		private async Task ExecuteUserIntendedChangesRun()
		{
			string versionId = $"Intended_Changes_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";
			switch (ProgramModelType)
			{
				case ProgramModelType.GitModel:
					versionId = gitCommitsProvider.GetCommitIdentifier(RepositoryPath, gitCommitsProvider.GetLatestCommitSha(RepositoryPath));
					break;
				case ProgramModelType.TFS2010Model:
					versionId = "Test";
					break;
			}

			var intendedChangesArtefact = new IntendedChangesArtefact
			{
				IntendedChanges = userRunConfigurationProvider.IntendedChanges,
				LocalProgramModel = new LocalProgramModel
				{
					GranularityLevel = GranularityLevel,
					AbsoluteSolutionPath = SolutionFilePath,
					VersionId = versionId
				}
			};

			await ExecuteDeltaBasedRunFixGranularityLevel<IntendedChangesArtefact, LocalProgramModel>(intendedChangesArtefact);
		}

		private async Task ExecuteDeltaBasedRunFixGranularityLevel<TDeltaArtefact, TModel>(TDeltaArtefact deltaArtefact)
			where TModel : CSharpProgramModel
		{
			switch (GranularityLevel)
			{
				/* TODO Granularity Level File
				 * 
				 * case GranularityLevel.File:
					await ExecuteRunFixProcessingType<TArtefact, TModel, StructuralDelta<TModel, CSharpFileElement>>(oldProgramArtefact, newProgramArtefact);
					break;*/
				case GranularityLevel.Class:
					await ExecuteDeltaBasedRunFixProcessingType<TDeltaArtefact, TModel, StructuralDelta<TModel, CSharpClassElement>>(deltaArtefact);
					break;
			}
		}

		private async Task ExecuteDeltaBasedRunFixProcessingType<TDeltaArtefact, TModel, TDelta>(TDeltaArtefact deltaArtefact)
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
		{
			switch (ProcessingType)
			{
				case ProcessingType.MSTestExecution:
				case ProcessingType.MSTestExecutionCreateCorrespondenceModel:
				case ProcessingType.MSTestExecutionLimitedTime:
					await ExecuteDeltaBasedRun<TDeltaArtefact, TModel, TDelta, MSTestTestcase, ITestsExecutionResult<MSTestTestcase>>(deltaArtefact);
					break;
				case ProcessingType.CsvReporting:
					var csvCreationResult = await ExecuteDeltaBasedRun<TDeltaArtefact, TModel, TDelta, MSTestTestcase, FileProcessingResult>(deltaArtefact);
					HandleCsvCreationResult(csvCreationResult);
					break;
				case ProcessingType.ListReporting:
					var listReportingResult = await ExecuteDeltaBasedRun<TDeltaArtefact, TModel, TDelta, MSTestTestcase, TestListResult<MSTestTestcase>>(deltaArtefact);
					HandleListReportingResult(listReportingResult);
					break;
			}
		}

		private async Task<TResult> ExecuteDeltaBasedRun<TDeltaArtefact, TModel, TDelta, TTestCase, TResult>(TDeltaArtefact deltaArtefact) where TTestCase : ITestCase
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
		{
			var deltaBasedController = UnityModelInitializer.GetDeltaBasedController<TDeltaArtefact, TModel, TDelta, TTestCase, TResult>(RTSApproachType, ProcessingType);

			deltaBasedController.ImpactedTest += HandleImpactedTest;
			deltaBasedController.TestResultAvailable += HandleTestExecutionResult;

			return
				await Task.Run(
					() => deltaBasedController.ExecuteImpactedTests(deltaArtefact, cancellationTokenSource.Token),
					cancellationTokenSource.Token);
		}

		#endregion

		#region StateBasedController

		private async Task ExecuteGitRun()
		{
			GitVersionIdentification newGitIdentification;

			var oldGitIdentification = new GitVersionIdentification
			{
				ReferenceType = GitVersionReferenceType.SpecificCommit,
				Commit = new GitCommit { ShaId = FromCommit.Identifier },
				RepositoryPath = RepositoryPath,
				AbsoluteSolutionPath = SolutionFilePath,
				GranularityLevel = GranularityLevel
			};

			if (ToCommit.Identifier == UncommittedChangesIdentifier)
			{
				newGitIdentification = new GitVersionIdentification
				{
					ReferenceType = GitVersionReferenceType.CurrentChanges,
					RepositoryPath = RepositoryPath,
					AbsoluteSolutionPath = SolutionFilePath,
					GranularityLevel = GranularityLevel
				};
			}
			else
			{
				newGitIdentification = new GitVersionIdentification
				{
					ReferenceType = GitVersionReferenceType.SpecificCommit,
					Commit = new GitCommit { ShaId = ToCommit.Identifier },
					RepositoryPath = RepositoryPath,
					AbsoluteSolutionPath = SolutionFilePath,
					GranularityLevel = GranularityLevel
				};
			}

			await ExecuteRunFixGranularityLevel<GitVersionIdentification, GitProgramModel>(oldGitIdentification, newGitIdentification);
		}

		private async Task ExecuteTFS2010Run()
		{
			var oldTfsProgramArtefact = new TFS2010VersionIdentification
			{
				AbsoluteSolutionPath = SolutionFilePath,
				GranularityLevel = GranularityLevel,
				CommitId = "Test"
			};
			var newTfsProgramArtefact = new TFS2010VersionIdentification
			{
				AbsoluteSolutionPath = SolutionFilePath,
				GranularityLevel = GranularityLevel,
				CommitId = "Test2"
			};

			await ExecuteRunFixGranularityLevel<TFS2010VersionIdentification, TFS2010ProgramModel>(oldTfsProgramArtefact, newTfsProgramArtefact);
		}

		private async Task ExecuteRunFixGranularityLevel<TArtefact, TModel>(TArtefact oldProgramArtefact, TArtefact newProgramArtefact)
			where TModel : CSharpProgramModel
		{
			switch (GranularityLevel)
			{
				/* TODO Granularity Level File
				 * 
				 * case GranularityLevel.File:
					await ExecuteRunFixProcessingType<TArtefact, TModel, StructuralDelta<TModel, CSharpFileElement>>(oldProgramArtefact, newProgramArtefact);
					break;*/
				case GranularityLevel.Class:
					await ExecuteRunFixProcessingType<TArtefact, TModel, StructuralDelta<TModel, CSharpClassElement>>(oldProgramArtefact, newProgramArtefact);
					break;
			}
		}

		private async Task ExecuteRunFixProcessingType<TArtefact, TModel, TDelta>(TArtefact oldArtefact, TArtefact newArtefact)
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
		{
			switch (ProcessingType)
			{
				case ProcessingType.MSTestExecution:
				case ProcessingType.MSTestExecutionCreateCorrespondenceModel:
				case ProcessingType.MSTestExecutionLimitedTime:
					await ExecuteRun<TArtefact, TModel, TDelta, MSTestTestcase, ITestsExecutionResult<MSTestTestcase>>(oldArtefact, newArtefact);
					break;
				case ProcessingType.CsvReporting:
					var csvCreationResult = await ExecuteRun<TArtefact, TModel, TDelta, MSTestTestcase, FileProcessingResult>(oldArtefact, newArtefact);
					HandleCsvCreationResult(csvCreationResult);
					break;
				case ProcessingType.ListReporting:
					var listReportingResult = await ExecuteRun<TArtefact, TModel, TDelta, MSTestTestcase, TestListResult<MSTestTestcase>>(oldArtefact, newArtefact);
					HandleListReportingResult(listReportingResult);
					break;
			}
		}

		private async Task<TResult> ExecuteRun<TArtefact, TModel, TDelta, TTestCase, TResult>(TArtefact oldArtefact, TArtefact newArtefact) where TTestCase : ITestCase
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
		{
			var stateBasedController = UnityModelInitializer.GetStateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult>(RTSApproachType, ProcessingType);

			stateBasedController.ImpactedTest += HandleImpactedTest;
			stateBasedController.TestResultAvailable += HandleTestExecutionResult;

			return
				await Task.Run(
					() => stateBasedController.ExecuteImpactedTests(oldArtefact, newArtefact, cancellationTokenSource.Token),
					cancellationTokenSource.Token);
		}

		#endregion

		#region HandlingResults

		private void HandleListReportingResult<TTestCase>(TestListResult<TTestCase> listReportingResult) where TTestCase : ITestCase
		{
			TestResults.Clear();
			TestResults.AddRange(listReportingResult.IdentifiedTests.Select(x => new TestResultListViewItemViewModel(dialogService)
			{
				FullyQualifiedName = x.Id,
				FullClassName = x.FullClassName,
				Name = x.Name,
				Categories = string.Join(",", x.Categories)
			}));
		}

		private void HandleCsvCreationResult(FileProcessingResult csvCreationResult)
		{
			bool openFile = dialogService.ShowQuestion($"CSV file was created at '{csvCreationResult.FilePath}'.{Environment.NewLine} Do you want to open the file?", "CSV File Created");
			if (openFile)
			{
				Process.Start(csvCreationResult.FilePath);
			}
		}

		private void HandleTestExecutionResult<TTestCase>(object sender, TestCaseResultEventArgs<TTestCase> args) where TTestCase : ITestCase
		{
			applicationUiExecutor.ExecuteOnUi(() => ProcessExecutionResult(args.TestResult));
		}

		private void HandleImpactedTest<TTestCase>(object sender, ImpactedTestEventArgs<TTestCase> args) where TTestCase : ITestCase
		{
			applicationUiExecutor.ExecuteOnUi(() =>
					TestResults.Add(new TestResultListViewItemViewModel(dialogService)
					{
						FullyQualifiedName = args.TestCase.Id,
						Name = args.TestCase.Name,
						FullClassName = args.TestCase.FullClassName,
						Categories = string.Join(",", args.TestCase.Categories)
					}));
		}

		private void ProcessExecutionResult<TTestCase>(ITestCaseResult<TTestCase> executionResult) where TTestCase : ITestCase
		{
			var currentTestViewModel = TestResults.Single(x => x.FullyQualifiedName == executionResult.TestCase.Id);

			if (executionResult.TestCase.IsChildTestCase)
			{
				currentTestViewModel.AddChildResults(new TestResultListViewItemViewModel(dialogService)
				{
					TestOutcome = executionResult.Outcome,
					StartTime = executionResult.StartTime,
					EndTime = executionResult.EndTime,
					DurationInSeconds = executionResult.DurationInSeconds,
					ErrorMessage = executionResult.ErrorMessage,
					StackTrace = executionResult.StackTrace,
					DisplayName = executionResult.DisplayName
				});

				currentTestViewModel.HasChildResults = true;
			}
			else
			{
				currentTestViewModel.TestOutcome = executionResult.Outcome;
				currentTestViewModel.StartTime = executionResult.StartTime;
				currentTestViewModel.EndTime = executionResult.EndTime;
				currentTestViewModel.DurationInSeconds = executionResult.DurationInSeconds;
				currentTestViewModel.ErrorMessage = executionResult.ErrorMessage;
				currentTestViewModel.StackTrace = executionResult.StackTrace;
			}
		}

		#endregion

	}
}