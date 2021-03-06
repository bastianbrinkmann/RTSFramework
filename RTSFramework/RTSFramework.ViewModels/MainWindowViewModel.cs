﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Msagl.Drawing;
using Prism.Commands;
using Prism.Mvvm;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Concrete.Reporting;
using RTSFramework.Concrete.TFS2010;
using RTSFramework.Concrete.User;
using RTSFramework.Concrete.User.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.Contracts.SecondaryFeature;
using RTSFramework.Contracts.Utilities;
using RTSFramework.Core;
using RTSFramework.Core.Models;
using RTSFramework.RTSApproaches.Core;
using RTSFramework.ViewModels.Controller;
using RTSFramework.ViewModels.RequireUIServices;
using RTSFramework.ViewModels.RunConfigurations;

namespace RTSFramework.ViewModels
{
	public class MainWindowViewModel : BindableBase
	{
		private const string UncommittedChangesIdentifier = "uncomittedChanges";

		private IArtefactBasedController<Graph> lastUsedController;

		private readonly IDialogService dialogService;
		private readonly IApplicationUiExecutor applicationUiExecutor;
		private readonly IUserRunConfigurationProvider userRunConfigurationProvider;
		private readonly IStatisticsReporter statisticsReporter;
		private readonly IArtefactAdapter<string, StatisticsReportData> reportArtefactAdapter;
		private readonly UserSettings userSettings;
		private readonly GitCommitsProvider gitCommitsProvider;
		private CancellationTokenSource cancellationTokenSource;

		#region BackingFields

		private ICommand startRunCommand;
		private ProcessingType processingType;
		private DiscoveryType discoveryType;
		private RTSApproachType rtsApproachType;
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
		private ProgramLocation programLocation;
		private bool isRepositoryPathChangable;
		private ObservableCollection<DiscoveryType> discoveryTypes;
		private bool withTimeLimit;
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
		private bool discoverNewTests;
		private DelegateCommand visualizeDependenciesCommand;
		private bool dependenciesVisualizationAvailable;
		private DelegateCommand reportCollectedStatisticsCommand;
		private double fontSize;

		#endregion

		public MainWindowViewModel(IDialogService dialogService,
			GitCommitsProvider gitCommitsProvider,
			IApplicationUiExecutor applicationUiExecutor,
			IUserRunConfigurationProvider userRunConfigurationProvider,
			UserSettingsProvider userRunSettingsProvider,
			IStatisticsReporter statisticsReporter,
			IArtefactAdapter<string, StatisticsReportData> reportArtefactAdapter,
			ISettingsProvider settingsProvider)
		{
			this.dialogService = dialogService;
			this.applicationUiExecutor = applicationUiExecutor;
			this.userRunConfigurationProvider = userRunConfigurationProvider;
			this.statisticsReporter = statisticsReporter;
			this.reportArtefactAdapter = reportArtefactAdapter;
			this.gitCommitsProvider = gitCommitsProvider;

			StartRunCommand = new DelegateCommand(ExecuteOfflineRunFixModel);
			CancelRunCommand = new DelegateCommand(CancelRun);
			SelectSolutionFileCommand = new DelegateCommand(SelectSolutionFile);
			SelectRepositoryCommand = new DelegateCommand(SelectRepository);
			SelectCsvTestsFileCommand = new DelegateCommand(SelectCsvTestsFile);
			SpecitfyIntendedChangesCommand = new DelegateCommand(SpecifyIntendedChanges);
			VisualizeDependenciesCommand = new DelegateCommand(VisualizeDependencies);
			ReportCollectedStatisticsCommand = new DelegateCommand(ReportCollectedStatistics);

			DiscoveryTypes = new ObservableCollection<DiscoveryType>();
			ProcessingTypes = new ObservableCollection<ProcessingType>();
			TestResults = new ObservableCollection<TestResultListViewItemViewModel>();
			FromCommitModels = new ObservableCollection<CommitViewModel>();
			ToCommitModels = new ObservableCollection<CommitViewModel>();

			RunStatus = RunStatus.Ready;

			PropertyChanged += OnPropertyChanged;

			userSettings = userRunSettingsProvider.GetUserSettings();

			var discoveryTypeFromSettings = userSettings.DiscoveryType;
			var processingTypeFromSettings = userSettings.ProcessingType;

			ProgramLocation = userSettings.ProgramLocation;
			TestType = userSettings.TestType;
			DiscoveryType = discoveryTypeFromSettings;
			ProcessingType = processingTypeFromSettings;
			RTSApproachType = userSettings.RTSApproachType;
			SolutionFilePath = userSettings.SolutionFilePath;
			RepositoryPath = userSettings.RepositoryPath;
			TimeLimit = userSettings.TimeLimit;
			WithTimeLimit = userSettings.WithTimeLimit;
			ClassNameFilter = userSettings.ClassNameFilter;
			TestCaseNameFilter = userSettings.TestCaseNameFilter;
			CategoryFilter = userSettings.CategoryFilter;
			CsvTestsFile = userSettings.CsvTestsFile;

			DiscoverNewTests = true;

			RegularGitsCommitRefresh();

			FontSize = settingsProvider.FontSize;
		}

		private void RegularGitsCommitRefresh()
		{
			Task.Run(async () =>
			{
				await Task.Delay(5000);
				if (ProgramLocation == ProgramLocation.GitRepository)
				{
					applicationUiExecutor.ExecuteOnUi(RefreshCommitsSelection);
				}

				RegularGitsCommitRefresh();
			});
		}

		private void ReportCollectedStatistics()
		{
			string reportArtefact = reportArtefactAdapter.Unparse(statisticsReporter.GetStatisticsReport());

			dialogService.ShowInformation(reportArtefact);
		}

		private void VisualizeDependencies()
		{
			var graph = lastUsedController.GetDependenciesVisualization();
			dialogService.ShowGraph(graph);
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
			var fromCommitId = FromCommit?.Identifier;

			FromCommitModels.Clear();
			FromCommitModels.AddRange(gitCommitsProvider.GetAllCommits(RepositoryPath).Select(ConvertCommit));
			var oldFromCommit = FromCommitModels.SingleOrDefault(x => x.Identifier == fromCommitId);

			FromCommit = oldFromCommit ?? FromCommitModels.FirstOrDefault();
			IsFromCommitChangeable = DiscoveryType == DiscoveryType.OfflineDiscovery && FromCommitModels.Any();
			IsToCommitChangeable = DiscoveryType == DiscoveryType.OfflineDiscovery && ToCommitModels.Any();
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
			if (ProgramLocation == ProgramLocation.GitRepository)
			{
				DiscoveryTypes.Clear();
				DiscoveryTypes.Add(DiscoveryType.OfflineDiscovery);
				DiscoveryTypes.Add(DiscoveryType.UserIntendedChangesDiscovery);
			}
			else if (ProgramLocation == ProgramLocation.LocalProgram)
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
					ProcessingTypes.Add(ProcessingType.ListReporting);
					ProcessingTypes.Add(ProcessingType.CsvReporting);
					ProcessingTypes.Add(ProcessingType.CollectStatistics);
					break;
				case TestType.CsvList:
					ProcessingTypes.Add(ProcessingType.ListReporting);
					ProcessingTypes.Add(ProcessingType.CsvReporting);
					ProcessingTypes.Add(ProcessingType.CollectStatistics);
					break;
			}

			ProcessingType = ProcessingTypes.FirstOrDefault();
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			switch (propertyChangedEventArgs.PropertyName)
			{
				case nameof(ProgramLocation):
					IsRepositoryPathChangable = ProgramLocation == ProgramLocation.GitRepository;
					RefreshDiscoveryTypes();
					break;
				case nameof(DiscoveryType):
					IsIntededChangesEditingEnabled = DiscoveryType == DiscoveryType.UserIntendedChangesDiscovery;
					IsFromCommitChangeable = DiscoveryType == DiscoveryType.OfflineDiscovery && FromCommitModels.Any();
					IsToCommitChangeable = DiscoveryType == DiscoveryType.OfflineDiscovery && ToCommitModels.Any();
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
					ToCommitModels.AddRange(gitCommitsProvider.GetAllCommits(RepositoryPath).TakeWhile(x => x.ShaId != FromCommit?.Identifier).Select(ConvertCommit));
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

		public double FontSize
		{
			get { return fontSize; }
			set
			{
				fontSize = value;
				RaisePropertyChanged();
			}
		}

		public bool DiscoverNewTests
		{
			get { return discoverNewTests; }
			set
			{
				discoverNewTests = value;
				userRunConfigurationProvider.DiscoverNewTests = value;
				RaisePropertyChanged();
			}
		}

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

		public bool WithTimeLimit
		{
			get { return withTimeLimit; }
			set
			{
				withTimeLimit = value;
				userSettings.WithTimeLimit = value;
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

		public ProgramLocation ProgramLocation
		{
			get { return programLocation; }
			set
			{
				programLocation = value;
				RaisePropertyChanged();
				userSettings.ProgramLocation = value;
			}
		}

		public DelegateCommand VisualizeDependenciesCommand
		{
			get { return visualizeDependenciesCommand; }
			set
			{
				visualizeDependenciesCommand = value;
				RaisePropertyChanged();
			}
		}

		public bool DependenciesVisualizationAvailable
		{
			get { return dependenciesVisualizationAvailable; }
			set
			{
				dependenciesVisualizationAvailable = value;
				RaisePropertyChanged();
			}
		}

		public DelegateCommand ReportCollectedStatisticsCommand
		{
			get { return reportCollectedStatisticsCommand; }
			set
			{
				reportCollectedStatisticsCommand = value;
				RaisePropertyChanged();
			}
		}

		#endregion

		private async void ExecuteOfflineRunFixModel()
		{
			RunStatus = RunStatus.Running;
			TestResults.Clear();

			cancellationTokenSource = new CancellationTokenSource();

			try
			{
				switch (DiscoveryType)
				{
					case DiscoveryType.OfflineDiscovery:
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
			switch (ProgramLocation)
			{
				case ProgramLocation.GitRepository:
					versionId = gitCommitsProvider.GetCommitIdentifier(RepositoryPath, gitCommitsProvider.GetLatestCommitSha(RepositoryPath));
					break;
				case ProgramLocation.LocalProgram:
					versionId = "Test";
					break;
			}

			var intendedChangesArtefact = new IntendedChangesArtefact
			{
				IntendedChanges = userRunConfigurationProvider.IntendedChanges,
				ProgramModel = new FilesProgramModel
				{
					AbsoluteSolutionPath = SolutionFilePath,
					VersionId = versionId
				}
			};

			await ExecuteDeltaBasedRunFixTestType<IntendedChangesArtefact, FilesProgramModel, StructuralDelta<FilesProgramModel, FileElement>>(intendedChangesArtefact);
		}

		private async Task ExecuteDeltaBasedRunFixTestType<TDeltaArtefact, TModel, TProgramDelta>(TDeltaArtefact deltaArtefact)
			where TModel : CSharpProgramModel
			where TProgramDelta : IDelta<TModel>
		{
			switch (TestType)
			{
				case TestType.MSTest:
					await ExecuteDeltaBasedRunFixProcessingType<TDeltaArtefact, TModel, TProgramDelta, MSTestTestcase>(deltaArtefact);
					break;
				case TestType.CsvList:
					await ExecuteDeltaBasedRunFixProcessingType<TDeltaArtefact, TModel, TProgramDelta, CsvFileTestcase>(deltaArtefact);
					break;
			}
		}

		private async Task ExecuteDeltaBasedRunFixProcessingType<TDeltaArtefact, TModel, TProgramDelta, TTestCase>(TDeltaArtefact deltaArtefact)
			where TModel : IProgramModel
			where TProgramDelta : IDelta<TModel>
			where TTestCase : ITestCase
		{
			switch (ProcessingType)
			{
				case ProcessingType.MSTestExecution:
				case ProcessingType.MSTestExecutionCreateCorrespondenceModel:
					await ExecuteDeltaBasedRun<TDeltaArtefact, TModel, TProgramDelta, MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, object>(deltaArtefact);
					break;
				case ProcessingType.CsvReporting:
					var csvCreationResult = await ExecuteDeltaBasedRun<TDeltaArtefact, TModel, TProgramDelta, TTestCase, TestListResult<TTestCase>, CsvFileArtefact>(deltaArtefact);
					HandleCsvCreationResult(csvCreationResult);
					break;
				case ProcessingType.ListReporting:
					var listReportingResult = await ExecuteDeltaBasedRun<TDeltaArtefact, TModel, TProgramDelta, TTestCase, TestListResult<TTestCase>, IList<TestResultListViewItemViewModel>>(deltaArtefact);
					HandleListReportingResult(listReportingResult);
					break;
				case ProcessingType.CollectStatistics:
					var statisticsResult = await ExecuteDeltaBasedRun<TDeltaArtefact, TModel, TProgramDelta, TTestCase, PercentageImpactedTestsStatistic, CsvFileArtefact>(deltaArtefact);
					HandleCsvCreationResult(statisticsResult);
					break;
			}
		}

		private async Task<TResultArtefact> ExecuteDeltaBasedRun<TDeltaArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact>(TDeltaArtefact deltaArtefact)
			where TModel : IProgramModel
			where TProgramDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
			where TTestCase : ITestCase
		{
			var deltaBasedController = UnityModelInitializer.GetDeltaBasedController<TDeltaArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, Graph>(RTSApproachType, ProcessingType, WithTimeLimit);

			deltaBasedController.FilterFunction = GetFilterFunction<TTestCase>();

			lastUsedController = deltaBasedController;

			deltaBasedController.ImpactedTest += HandleImpactedTest;
			deltaBasedController.TestResultAvailable += HandleTestExecutionResult;
			deltaBasedController.TestsPrioritized += HandleTestsPrioritized;

			var result = await Task.Run(() => deltaBasedController.ExecuteRTSRun(deltaArtefact, cancellationTokenSource.Token), cancellationTokenSource.Token);

			DependenciesVisualizationAvailable = true;
			deltaBasedController.ImpactedTest -= HandleImpactedTest;
			deltaBasedController.TestResultAvailable -= HandleTestExecutionResult;
			deltaBasedController.TestsPrioritized -= HandleTestsPrioritized;

			return result;
		}

		#endregion

		#region OfflineController

		private async Task ExecuteGitRun()
		{
			GitVersionIdentification newGitIdentification;

			var oldGitIdentification = new GitVersionIdentification
			{
				ReferenceType = GitVersionReferenceType.SpecificCommit,
				Commit = new GitCommit { ShaId = FromCommit.Identifier },
				RepositoryPath = RepositoryPath,
				AbsoluteSolutionPath = SolutionFilePath
			};

			if (ToCommit.Identifier == UncommittedChangesIdentifier)
			{
				newGitIdentification = new GitVersionIdentification
				{
					ReferenceType = GitVersionReferenceType.CurrentChanges,
					RepositoryPath = RepositoryPath,
					AbsoluteSolutionPath = SolutionFilePath
				};
			}
			else
			{
				newGitIdentification = new GitVersionIdentification
				{
					ReferenceType = GitVersionReferenceType.SpecificCommit,
					Commit = new GitCommit { ShaId = ToCommit.Identifier },
					RepositoryPath = RepositoryPath,
					AbsoluteSolutionPath = SolutionFilePath
				};
			}

			await ExecuteOfflineRunFixTestType<GitVersionIdentification, FilesProgramModel, StructuralDelta<FilesProgramModel, FileElement>>(oldGitIdentification, newGitIdentification);
		}

		private async Task ExecuteOfflineRunFixTestType<TArtefact, TModel, TProgramDelta>(TArtefact oldProgramArtefact, TArtefact newProgramArtefact)
			where TModel : CSharpProgramModel
			where TProgramDelta : IDelta<TModel>
		{
			switch (TestType)
			{
				case TestType.MSTest:
					await ExecuteOfflineRunFixProcessingType<TArtefact, TModel, TProgramDelta, MSTestTestcase>(oldProgramArtefact, newProgramArtefact);
					break;
				case TestType.CsvList:
					await ExecuteOfflineRunFixProcessingType<TArtefact, TModel, TProgramDelta, CsvFileTestcase>(oldProgramArtefact, newProgramArtefact);
					break;
			}
		}

		private async Task ExecuteOfflineRunFixProcessingType<TArtefact, TModel, TProgramDelta, TTestCase>(TArtefact oldArtefact, TArtefact newArtefact)
			where TModel : IProgramModel
			where TProgramDelta : IDelta<TModel>
			where TTestCase : ITestCase
		{
			switch (ProcessingType)
			{
				case ProcessingType.MSTestExecution:
				case ProcessingType.MSTestExecutionCreateCorrespondenceModel:
					await ExecuteOfflineRun<TArtefact, TModel, TProgramDelta, MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, object>(oldArtefact, newArtefact);
					break;
				case ProcessingType.CsvReporting:
					var csvCreationResult = await ExecuteOfflineRun<TArtefact, TModel, TProgramDelta, TTestCase, TestListResult<TTestCase>, CsvFileArtefact>(oldArtefact, newArtefact);
					HandleCsvCreationResult(csvCreationResult);
					break;
				case ProcessingType.ListReporting:
					var listReportingResult = await ExecuteOfflineRun<TArtefact, TModel, TProgramDelta, TTestCase, TestListResult<TTestCase>, IList<TestResultListViewItemViewModel>>(oldArtefact, newArtefact);
					HandleListReportingResult(listReportingResult);
					break;
				case ProcessingType.CollectStatistics:
					var statisticsResult = await ExecuteOfflineRun<TArtefact, TModel, TProgramDelta, TTestCase, PercentageImpactedTestsStatistic, CsvFileArtefact>(oldArtefact, newArtefact);
					HandleCsvCreationResult(statisticsResult);
					break;
			}
		}

		private async Task<TResultArtefact> ExecuteOfflineRun<TArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact>(TArtefact oldArtefact, TArtefact newArtefact)
			where TModel : IProgramModel
			where TProgramDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
			where TTestCase : ITestCase
		{
			var offlineController = UnityModelInitializer.GetOfflineController<TArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, Graph>(RTSApproachType, ProcessingType, WithTimeLimit);

			lastUsedController = offlineController;

			offlineController.FilterFunction = GetFilterFunction<TTestCase>();

			offlineController.ImpactedTest += HandleImpactedTest;
			offlineController.TestResultAvailable += HandleTestExecutionResult;
			offlineController.TestsPrioritized += HandleTestsPrioritized;

			var result = await Task.Run(() => offlineController.ExecuteRTSRun(oldArtefact, newArtefact, cancellationTokenSource.Token), cancellationTokenSource.Token);

			DependenciesVisualizationAvailable = true;
			offlineController.ImpactedTest -= HandleImpactedTest;
			offlineController.TestResultAvailable -= HandleTestExecutionResult;
			offlineController.TestsPrioritized -= HandleTestsPrioritized;

			return result;
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
				filterFunction = x => previousFunc(x) && x.AssociatedClasses.Any(y => y.Contains(ClassNameFilter));
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
			for (int i = 0; i < eventArgs.TestCases.Count; i++)
			{
				int testCaseId = i;
				applicationUiExecutor.ExecuteOnUi(() =>
				{
					var testCase = eventArgs.TestCases[testCaseId];

					var currentTestViewModel = TestResults.Single(x => x.FullyQualifiedName == testCase.Id);
					currentTestViewModel.ExecutionId = testCaseId;
				});
			}

			applicationUiExecutor.ExecuteOnUi(() =>
			{
				var allTestsViewModels = TestResults.OrderBy(x => x.ExecutionId).ToList();
				TestResults.Clear();
				TestResults.AddRange(allTestsViewModels);
			});
		}

		private void HandleListReportingResult(IList<TestResultListViewItemViewModel> viewModels)
		{

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
						FullClassName = string.Join(",", args.TestCase.AssociatedClasses),
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