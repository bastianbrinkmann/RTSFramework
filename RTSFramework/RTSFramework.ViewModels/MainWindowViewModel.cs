using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Configuration;
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
using RTSFramework.Core.Models;
using RTSFramework.RTSApproaches.Core;
using RTSFramework.RTSApproaches.Dynamic;
using RTSFramework.ViewModels.Controller;
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
		private readonly UserSettings userSettings;
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
		private TestResultListViewItemViewModel selectedTest;
		private string testCaseNameFilter;
		private string classNameFilter;
		private string categoryFilter;
		private TestType testType;
		private ObservableCollection<ProcessingType> processingTypes;
		private ICommand selectCsvTestsFileCommand;
		private string csvTestsFile;
		private bool isCsvTestsFileSelectable;

		#endregion

		public MainWindowViewModel(IDialogService dialogService,
			GitCommitsProvider gitCommitsProvider,
			IApplicationUiExecutor applicationUiExecutor,
			IUserRunConfigurationProvider userRunConfigurationProvider,
			UserSettingsProvider userRunSettingsProvider)
		{
			this.dialogService = dialogService;
			this.applicationUiExecutor = applicationUiExecutor;
			this.userRunConfigurationProvider = userRunConfigurationProvider;
			this.gitCommitsProvider = gitCommitsProvider;

			StartRunCommand = new DelegateCommand(ExecuteRunFixModel);
			CancelRunCommand = new DelegateCommand(CancelRun);
			SelectSolutionFileCommand = new DelegateCommand(SelectSolutionFile);
			SelectRepositoryCommand = new DelegateCommand(SelectRepository);
			SelectCsvTestsFileCommand = new DelegateCommand(SelectCsvTestsFile);
			SpecitfyIntendedChangesCommand = new DelegateCommand(SpecifyIntendedChanges);

			DiscoveryTypes = new ObservableCollection<DiscoveryType>();
			ProcessingTypes = new ObservableCollection<ProcessingType>();
			TestResults = new ObservableCollection<TestResultListViewItemViewModel>();
			FromCommitModels = new ObservableCollection<CommitViewModel>();
			ToCommitModels = new ObservableCollection<CommitViewModel>();

			RunStatus = RunStatus.Ready;

			PropertyChanged += OnPropertyChanged;

			userSettings = userRunSettingsProvider.GetUserSettings();

			ProgramModelType = userSettings.ProgramModelType;
			TestType = userSettings.TestType;
			DiscoveryType = userSettings.DiscoveryType;
			ProcessingType = userSettings.ProcessingType;
			RTSApproachType = userSettings.RTSApproachType;
			GranularityLevel = userSettings.GranularityLevel;
			SolutionFilePath = userSettings.SolutionFilePath;
			RepositoryPath = userSettings.RepositoryPath;
			TimeLimit = userSettings.TimeLimit;
			ClassNameFilter = userSettings.ClassNameFilter;
			TestCaseNameFilter = userSettings.TestCaseNameFilter;
			CategoryFilter = userSettings.CategoryFilter;
			CsvTestsFile = userSettings.CsvTestsFile;
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

		private void SelectCsvTestsFile()
		{
			string selectedFile;
			if (dialogService.SelectFile(Environment.CurrentDirectory, "CSV Files (*.csv)|*.csv", out selectedFile))
			{
				CsvTestsFile = selectedFile;
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

		private void RefreshProcessingTypes()
		{
			ProcessingTypes.Clear();

			switch (TestType)
			{
				case TestType.MSTest:
					ProcessingTypes.Add(ProcessingType.MSTestExecution);
					ProcessingTypes.Add(ProcessingType.MSTestExecutionCreateCorrespondenceModel);
					ProcessingTypes.Add(ProcessingType.MSTestExecutionLimitedTime);
					ProcessingTypes.Add(ProcessingType.ListReporting);
					ProcessingTypes.Add(ProcessingType.CsvReporting);
					break;
				case TestType.CsvList:
					ProcessingTypes.Add(ProcessingType.ListReporting);
					ProcessingTypes.Add(ProcessingType.CsvReporting);
					break;
			}

			ProcessingType = ProcessingTypes.FirstOrDefault();
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
				case nameof(TestType):
					RefreshProcessingTypes();
					IsCsvTestsFileSelectable = TestType == TestType.CsvList;
					break;
				case nameof(CsvTestsFile):
					userRunConfigurationProvider.CsvTestsFile = CsvTestsFile;
					break;
			}
		}

		#region Properties

		public bool IsCsvTestsFileSelectable
		{
			get { return isCsvTestsFileSelectable; }
			set
			{
				isCsvTestsFileSelectable = value;
				RaisePropertyChanged();
			}
		}

		public string CsvTestsFile
		{
			get { return csvTestsFile; }
			set
			{
				csvTestsFile = value;
				RaisePropertyChanged();
				userSettings.CsvTestsFile = CsvTestsFile;
			}
		}

		public ICommand SelectCsvTestsFileCommand
		{
			get { return selectCsvTestsFileCommand; }
			set
			{
				selectCsvTestsFileCommand = value;
				RaisePropertyChanged();
			}
		}

		public ObservableCollection<ProcessingType> ProcessingTypes
		{
			get { return processingTypes; }
			set
			{
				processingTypes = value;
				RaisePropertyChanged();
			}
		}

		public TestType TestType
		{
			get { return testType; }
			set
			{
				testType = value;
				RaisePropertyChanged();
				userSettings.TestType = value;
			}
		}

		public string CategoryFilter
		{
			get { return categoryFilter; }
			set
			{
				categoryFilter = value;
				RaisePropertyChanged();
				userSettings.CategoryFilter = value;
			}
		}

		public string ClassNameFilter
		{
			get { return classNameFilter; }
			set
			{
				classNameFilter = value;
				RaisePropertyChanged();
				userSettings.ClassNameFilter = value;
			}
		}

		public string TestCaseNameFilter
		{
			get { return testCaseNameFilter; }
			set
			{
				testCaseNameFilter = value;
				RaisePropertyChanged();
				userSettings.TestCaseNameFilter = value;
			}
		}

		public TestResultListViewItemViewModel SelectedTest
		{
			get { return selectedTest; }
			set
			{
				selectedTest = value;
				RaisePropertyChanged();
			}
		}

		public double TimeLimit
		{
			get { return timeLimit; }
			set
			{
				timeLimit = value;
				RaisePropertyChanged();
				userSettings.TimeLimit = value;
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
				userSettings.RepositoryPath = value;
			}
		}

		public string SolutionFilePath
		{
			get { return solutionFilePath; }
			set
			{
				solutionFilePath = value;
				RaisePropertyChanged();
				userSettings.SolutionFilePath = value;
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
				userSettings.GranularityLevel = value;
			}
		}

		public RTSApproachType RTSApproachType
		{
			get { return rtsApproachType; }
			set
			{
				rtsApproachType = value;
				RaisePropertyChanged();
				userSettings.RTSApproachType = value;
			}
		}

		public DiscoveryType DiscoveryType
		{
			get { return discoveryType; }
			set
			{
				discoveryType = value;
				RaisePropertyChanged();
				userSettings.DiscoveryType = value;
			}
		}

		public ProcessingType ProcessingType
		{
			get { return processingType; }
			set
			{
				processingType = value;
				RaisePropertyChanged();
				userSettings.ProcessingType = value;
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
				userSettings.ProgramModelType = value;
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
			catch (TerminationConditionReachedException)
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

			await ExecuteDeltaBasedRunFixSelectionDelta<IntendedChangesArtefact, LocalProgramModel, StructuralDelta<LocalProgramModel, FileElement>>(intendedChangesArtefact);
		}

		private async Task ExecuteDeltaBasedRunFixSelectionDelta<TDeltaArtefact, TModel, TParsedDelta>(TDeltaArtefact deltaArtefact)
			where TModel : CSharpProgramModel 
			where TParsedDelta : IDelta<TModel>
		{
			switch (GranularityLevel)
			{
				/* TODO Granularity Level File
				 * 
				 * case GranularityLevel.File:
					await ExecuteRunFixProcessingType<TArtefact, TModel, StructuralDelta<TModel, CSharpFileElement>>(oldProgramArtefact, newProgramArtefact);
					break;*/
				case GranularityLevel.Class:
					await ExecuteDeltaBasedRunFixTestType<TDeltaArtefact, TModel, TParsedDelta, StructuralDelta<TModel, CSharpClassElement>>(deltaArtefact);
					break;
			}
		}

		private async Task ExecuteDeltaBasedRunFixTestType<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta>(TDeltaArtefact deltaArtefact)
			where TModel : CSharpProgramModel
			where TParsedDelta : IDelta<TModel>
			where TSelectionDelta : IDelta<TModel>
		{
			switch (TestType)
			{
				case TestType.MSTest:
					await ExecuteDeltaBasedRunFixProcessingType<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, MSTestTestcase>(deltaArtefact);
					break;
				case TestType.CsvList:
					await ExecuteDeltaBasedRunFixProcessingType<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, CsvFileTestcase>(deltaArtefact);
					break;
			}
		}

		private async Task ExecuteDeltaBasedRunFixProcessingType<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase>(TDeltaArtefact deltaArtefact)
			where TModel : IProgramModel
			where TParsedDelta : IDelta<TModel>
			where TSelectionDelta : IDelta<TModel>
			where TTestCase : ITestCase
		{
			switch (ProcessingType)
			{
				case ProcessingType.MSTestExecution:
				case ProcessingType.MSTestExecutionCreateCorrespondenceModel:
				case ProcessingType.MSTestExecutionLimitedTime:
					await ExecuteDeltaBasedRun<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, object>(deltaArtefact);
					break;
				case ProcessingType.CsvReporting:
					var csvCreationResult = await ExecuteDeltaBasedRun<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TestListResult<TTestCase>, CsvFileArtefact>(deltaArtefact);
					HandleCsvCreationResult(csvCreationResult);
					break;
				case ProcessingType.ListReporting:
					var listReportingResult = await ExecuteDeltaBasedRun<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TestListResult<TTestCase>, IList<TestResultListViewItemViewModel>>(deltaArtefact);
					HandleListReportingResult(listReportingResult);
					break;
			}
		}

		private async Task<TResultArtefact> ExecuteDeltaBasedRun<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact>(TDeltaArtefact deltaArtefact)
			where TModel : IProgramModel
			where TParsedDelta : IDelta<TModel>
			where TSelectionDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
			where TTestCase : ITestCase
		{
			var deltaBasedController = UnityModelInitializer.GetDeltaBasedController<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact>(RTSApproachType, ProcessingType);

			deltaBasedController.FilterFunction = GetFilterFunction<TTestCase>();

			deltaBasedController.DeltaArtefact = deltaArtefact;

			deltaBasedController.ImpactedTest += HandleImpactedTest;
			deltaBasedController.TestResultAvailable += HandleTestExecutionResult;
			deltaBasedController.TestsPrioritized += HandleTestsPrioritized;

			await Task.Run(() => deltaBasedController.ExecuteRTSRun(cancellationTokenSource.Token), cancellationTokenSource.Token);
			return deltaBasedController.Result;
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

			await ExecuteRunFixSelectionDelta<GitVersionIdentification, GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>(oldGitIdentification, newGitIdentification);
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

			await ExecuteRunFixSelectionDelta<TFS2010VersionIdentification, TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, FileElement>>(oldTfsProgramArtefact, newTfsProgramArtefact);
		}
		private async Task ExecuteRunFixSelectionDelta<TArtefact, TModel, TDiscoveryDelta>(TArtefact oldProgramArtefact, TArtefact newProgramArtefact)
			where TModel : CSharpProgramModel
			where TDiscoveryDelta : IDelta<TModel>
		{
			switch (GranularityLevel)
			{
				/* TODO Granularity Level File
				 * 
				 * case GranularityLevel.File:
					await ExecuteRunFixProcessingType<TArtefact, TModel, StructuralDelta<TModel, CSharpFileElement>>(oldProgramArtefact, newProgramArtefact);
					break;*/
				case GranularityLevel.Class:
					await ExecuteRunFixTestType<TArtefact, TModel, TDiscoveryDelta, StructuralDelta<TModel, CSharpClassElement>>(oldProgramArtefact, newProgramArtefact);
					break;
			}
		}

		private async Task ExecuteRunFixTestType<TArtefact, TModel, TDeltaDiscovery, TDeltaSelection>(TArtefact oldProgramArtefact, TArtefact newProgramArtefact)
			where TModel : CSharpProgramModel
			where TDeltaDiscovery : IDelta<TModel>
			where TDeltaSelection : IDelta<TModel>
		{
			switch (TestType)
			{
				case TestType.MSTest:
					await ExecuteRunFixProcessingType<TArtefact, TModel, TDeltaDiscovery, TDeltaSelection, MSTestTestcase>(oldProgramArtefact, newProgramArtefact);
					break;
				case TestType.CsvList:
					await ExecuteRunFixProcessingType<TArtefact, TModel, TDeltaDiscovery, TDeltaSelection, CsvFileTestcase>(oldProgramArtefact, newProgramArtefact);
					break;
			}
		}

		private async Task ExecuteRunFixProcessingType<TArtefact, TModel, TDeltaDiscovery, TDeltaSelection, TTestCase>(TArtefact oldArtefact, TArtefact newArtefact)
			where TModel : IProgramModel
			where TDeltaDiscovery : IDelta<TModel>
			where TDeltaSelection : IDelta<TModel>
			where TTestCase : ITestCase
		{
			switch (ProcessingType)
			{
				case ProcessingType.MSTestExecution:
				case ProcessingType.MSTestExecutionCreateCorrespondenceModel:
				case ProcessingType.MSTestExecutionLimitedTime:
					await ExecuteRun<TArtefact, TModel, TDeltaDiscovery, TDeltaSelection, MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, object>(oldArtefact, newArtefact);
					break;
				case ProcessingType.CsvReporting:
					var csvCreationResult = await ExecuteRun<TArtefact, TModel, TDeltaDiscovery, TDeltaSelection, TTestCase, TestListResult<TTestCase>, CsvFileArtefact>(oldArtefact, newArtefact);
					HandleCsvCreationResult(csvCreationResult);
					break;
				case ProcessingType.ListReporting:
					var listReportingResult = await ExecuteRun<TArtefact, TModel, TDeltaDiscovery, TDeltaSelection, TTestCase, TestListResult<TTestCase>, IList<TestResultListViewItemViewModel>>(oldArtefact, newArtefact);
					HandleListReportingResult(listReportingResult);
					break;
			}
		}

		private async Task<TResultArtefact> ExecuteRun<TArtefact, TModel, TDeltaDiscovery, TDeltaSelection, TTestCase, TResult, TResultArtefact>(TArtefact oldArtefact, TArtefact newArtefact)
			where TModel : IProgramModel
			where TDeltaDiscovery : IDelta<TModel>
			where TDeltaSelection : IDelta<TModel>
			where TResult : ITestProcessingResult
			where TTestCase : ITestCase
		{
			var stateBasedController = UnityModelInitializer.GetStateBasedController<TArtefact, TModel, TDeltaDiscovery, TDeltaSelection, TTestCase, TResult, TResultArtefact>(RTSApproachType, ProcessingType);

			stateBasedController.FilterFunction = GetFilterFunction<TTestCase>();

			stateBasedController.OldArtefact = oldArtefact;
			stateBasedController.NewArtefact = newArtefact;

			stateBasedController.ImpactedTest += HandleImpactedTest;
			stateBasedController.TestResultAvailable += HandleTestExecutionResult;
			stateBasedController.TestsPrioritized += HandleTestsPrioritized;

			await Task.Run(() => stateBasedController.ExecuteRTSRun(cancellationTokenSource.Token), cancellationTokenSource.Token);
			return stateBasedController.Result;
		}

		#endregion

		private Func<TTestCase, bool> GetFilterFunction<TTestCase>()
			where TTestCase : ITestCase
		{
			Func<TTestCase, bool> filterFunction = x => true;

			if (!string.IsNullOrEmpty(TestCaseNameFilter))
			{
				var previousFunc = filterFunction;
				filterFunction = x => previousFunc(x) && x.Name.Contains(TestCaseNameFilter);
			}
			if (!string.IsNullOrEmpty(ClassNameFilter))
			{
				var previousFunc = filterFunction;
				filterFunction = x => previousFunc(x) && x.AssociatedClass.Contains(ClassNameFilter);
			}
			if (!string.IsNullOrEmpty(CategoryFilter))
			{
				var previousFunc = filterFunction;
				filterFunction = x => previousFunc(x) && x.Categories.Any(y => y.Contains(CategoryFilter));
			}

			return filterFunction;
		}

		#region HandlingResults

		private void HandleTestsPrioritized<TTestCase>(object sender, TestsPrioritizedEventArgs<TTestCase> eventArgs) where TTestCase : ITestCase
		{
			applicationUiExecutor.ExecuteOnUi(() =>
			{
				for (int i = 0; i < eventArgs.TestCases.Count; i++)
				{
					var testCase = eventArgs.TestCases[i];

					var currentTestViewModel = TestResults.Single(x => x.FullyQualifiedName == testCase.Id);
					currentTestViewModel.ExecutionId = i;
				}
				var allTestsViewModels = TestResults.OrderBy(x => x.ExecutionId).ToList();
				TestResults.Clear();
				TestResults.AddRange(allTestsViewModels);
			});
		}

		private void HandleListReportingResult(IList<TestResultListViewItemViewModel> viewModels)
		{
			TestResults.Clear();
			TestResults.AddRange(viewModels);
		}

		private void HandleCsvCreationResult(CsvFileArtefact fileArtefact)
		{
			bool openFile = dialogService.ShowQuestion($"CSV file was created at '{fileArtefact.CsvFilePath}'.{Environment.NewLine} Do you want to open the file?", "CSV File Created");
			if (openFile)
			{
				Process.Start(fileArtefact.CsvFilePath);
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
						FullClassName = args.TestCase.AssociatedClass,
						Categories = string.Join(",", args.TestCase.Categories),
						ResponsibleChanges =  args.ResponsibleChanges
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